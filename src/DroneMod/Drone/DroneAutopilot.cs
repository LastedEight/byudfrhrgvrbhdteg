using UnityEngine;
using DroneMod.Config;
using DroneMod.Input;
using DroneMod.Util;

namespace DroneMod.Drone
{
    /// <summary>
    /// Return-to-home and auto-land, plus the failsafes that trigger them.
    ///
    /// It works by synthesising the same <see cref="DroneInputState"/> a pilot would
    /// produce, so the flight model does not need to know whether a human or the
    /// autopilot is flying. RTH runs the standard three phases: climb to a safe
    /// altitude, track home, then descend.
    /// </summary>
    internal sealed class DroneAutopilot : MonoBehaviour
    {
        private enum Phase
        {
            Climb,
            Transit,
            Descend
        }

        /// <summary>Height above the launch point that RTH climbs to before transiting, metres.</summary>
        private const float SafeAltitude = 60f;

        private const float TransitSpeed = 18f;
        private const float DescentRate = 2.5f;
        private const float ArrivalRadius = 8f;

        private DroneController _drone;
        private Rigidbody _body;
        private Phase _phase;
        private float _failsafeAnnounced;

        public AutoMode Mode { get; private set; }

        /// <summary>Set when a failsafe rather than the pilot started the current auto mode.</summary>
        public string FailsafeReason { get; private set; }

        public void Initialise(DroneController drone)
        {
            _drone = drone;
            _body = drone != null ? drone.GetComponent<Rigidbody>() : null;
            Mode = AutoMode.Off;
        }

        public void Begin(AutoMode mode)
        {
            if (_drone == null)
            {
                return;
            }

            Mode = mode;
            _phase = Phase.Climb;

            if (mode == AutoMode.Land)
            {
                _phase = Phase.Descend;
            }

            ModLog.Info("Autopilot engaged: " + mode + ".");
        }

        public void Cancel()
        {
            if (Mode != AutoMode.Off)
            {
                ModLog.Info("Autopilot disengaged.");
            }

            Mode = AutoMode.Off;
            FailsafeReason = null;
        }

        /// <summary>
        /// Link loss and a critical battery both take the drone off the pilot. Both
        /// are configurable, because "let it drop where it is" is a legitimate
        /// preference when the alternative is the drone flying itself into a hill.
        /// </summary>
        public void EvaluateFailsafes()
        {
            if (_drone == null || !_drone.IsAlive)
            {
                return;
            }

            DroneLink link = _drone.Link;
            DroneBattery battery = _drone.Battery;

            bool linkLost = link != null && link.IsLost;
            bool batteryCritical = battery != null && battery.IsCritical;

            if (Mode != AutoMode.Off)
            {
                // Once the link comes back, hand control straight back to the pilot
                // unless the battery is what put us here.
                if (FailsafeReason == "link" && !linkLost && !batteryCritical)
                {
                    Cancel();
                }

                return;
            }

            if (batteryCritical)
            {
                FailsafeReason = "battery";
                Announce("BATTERY CRITICAL");
                Begin(ModConfig.FailsafeReturnHome && _drone.DistanceToHome < ModConfig.LinkRange * 0.5f
                    ? AutoMode.ReturnHome
                    : AutoMode.Land);
                return;
            }

            if (linkLost && ModConfig.FailsafeReturnHome)
            {
                FailsafeReason = "link";
                Announce("LINK LOST");
                Begin(AutoMode.ReturnHome);
            }
        }

        private void Announce(string message)
        {
            if (Time.time - _failsafeAnnounced < 5f)
            {
                return;
            }

            _failsafeAnnounced = Time.time;
            ModLog.Info("Failsafe: " + message);
        }

        /// <summary>
        /// Produces the stick demand for this physics step. The pilot's own input is
        /// passed in so gimbal and zoom control stay live -- you can still look
        /// around while the drone flies itself home.
        /// </summary>
        public DroneInputState ComputeInput(DroneInputState pilot, float deltaTime)
        {
            DroneInputState state = DroneInputState.Neutral;
            state.GimbalPitch = pilot.GimbalPitch;
            state.GimbalYaw = pilot.GimbalYaw;
            state.Zoom = pilot.Zoom;
            state.GimbalModifierHeld = pilot.GimbalModifierHeld;

            if (_drone == null)
            {
                return state;
            }

            Vector3 position = _drone.transform.position;
            Vector3 home = _drone.HomePoint;

            Vector3 toHome = home - position;
            Vector3 flatToHome = new Vector3(toHome.x, 0f, toHome.z);
            float groundDistance = flatToHome.magnitude;

            float targetAltitude;

            switch (_phase)
            {
                case Phase.Climb:
                    targetAltitude = home.y + SafeAltitude;
                    if (position.y >= targetAltitude - 4f || Mode == AutoMode.Land)
                    {
                        _phase = Phase.Transit;
                    }

                    break;

                case Phase.Transit:
                    targetAltitude = home.y + SafeAltitude;
                    if (groundDistance < ArrivalRadius)
                    {
                        _phase = Phase.Descend;
                    }

                    break;

                default:
                    targetAltitude = home.y;
                    break;
            }

            // Vertical: a simple proportional climb-rate demand, expressed as a
            // throttle-stick deflection because that is what altitude hold expects.
            float altitudeError = targetAltitude - position.y;
            float climbDemand = Mathf.Clamp(altitudeError * 0.25f, -DescentRate, ModConfig.MaxClimbRate);
            state.ThrottleStick = Mathf.Clamp(climbDemand / Mathf.Max(0.1f, ModConfig.MaxClimbRate), -1f, 1f);
            state.ThrottleAbsolute = Mathf.Clamp01(0.5f + state.ThrottleStick * 0.3f);

            if (_phase == Phase.Descend && _drone.AltitudeAgl < 0.4f)
            {
                // Touchdown: cut the demand and let it settle.
                state.ThrottleStick = -1f;
                state.ThrottleAbsolute = 0f;
                if (Mode != AutoMode.Off && _drone.GroundSpeed < 0.5f)
                {
                    Cancel();
                }

                return state;
            }

            if (_phase == Phase.Climb)
            {
                return state;
            }

            // Horizontal: aim for a velocity toward home, then convert the velocity
            // error into the pitch and roll the flight model understands.
            Vector3 desiredVelocity = groundDistance > 0.1f
                ? flatToHome / groundDistance * Mathf.Min(TransitSpeed, groundDistance * 0.6f)
                : Vector3.zero;

            Vector3 currentVelocity = _body != null ? _body.velocity : Vector3.zero;
            currentVelocity.y = 0f;

            Vector3 velocityError = desiredVelocity - currentVelocity;
            Vector3 localError = _drone.transform.InverseTransformDirection(velocityError);

            state.Pitch = Mathf.Clamp(localError.z * 0.14f, -0.85f, 0.85f);
            state.Roll = Mathf.Clamp(localError.x * 0.14f, -0.85f, 0.85f);

            // Point the nose at home so the pilot sees where they are going.
            if (groundDistance > ArrivalRadius)
            {
                float bearing = MathUtil.HeadingOf(flatToHome);
                float headingError = MathUtil.WrapAngle(bearing - _drone.Heading);
                state.Yaw = Mathf.Clamp(headingError * 0.02f, -0.6f, 0.6f);
            }

            return state;
        }

        /// <summary>Short label for the OSD, e.g. "RTH CLIMB".</summary>
        public string StatusLabel
        {
            get
            {
                if (Mode == AutoMode.Off)
                {
                    return string.Empty;
                }

                string prefix = Mode == AutoMode.ReturnHome ? "RTH" : "LAND";
                switch (_phase)
                {
                    case Phase.Climb:
                        return prefix + " CLIMB";
                    case Phase.Transit:
                        return prefix + " TRANSIT";
                    default:
                        return prefix + " DESCEND";
                }
            }
        }
    }
}
