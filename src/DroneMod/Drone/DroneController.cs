using UnityEngine;
using DroneMod.Config;
using DroneMod.Core;
using DroneMod.Input;
using DroneMod.Util;

namespace DroneMod.Drone
{
    /// <summary>
    /// The flight model and the flight controller, which on a real quad are the
    /// airframe and the firmware respectively.
    ///
    /// Lift is applied as a single force along the airframe's up axis -- that is how
    /// a multirotor actually works, and it is what makes the handling feel right:
    /// you cannot move horizontally without tilting, and tilting costs you lift.
    /// Everything else (drag, ground effect, wind, battery sag) sits on top of that.
    /// </summary>
    internal sealed class DroneController : MonoBehaviour
    {
        // Per-axis quadratic drag, as acceleration per (m/s)^2. Y is largest because
        // a quad presents its whole disc to vertical airflow.
        private static readonly Vector3 DragCoefficients = new Vector3(0.016f, 0.030f, 0.010f);

        private const float MaxAngularAccel = 45f;
        private const float AngularDamping = 2.6f;
        private const float CrashSpeed = 11f;
        private const float RotorSpan = DroneBuilder.ArmLength * 2f;
        private const int InputBufferSize = 64;

        private Rigidbody _body;
        private int _groundCheckCounter;
        private DroneCameraRig _camera;
        private DroneBattery _battery;
        private DroneLink _link;
        private DroneAudio _audio;
        private DroneAutopilot _autopilot;
        private PropSpinner[] _props;

        private readonly PidController _pitchPid = new PidController(0.055f, 0.010f, 0.012f, 12f, 1f);
        private readonly PidController _rollPid = new PidController(0.055f, 0.010f, 0.012f, 12f, 1f);
        private readonly PidController _yawPid = new PidController(0.0075f, 0.0020f, 0.0008f, 90f, 1f);
        private readonly PidController _climbPid = new PidController(2.6f, 0.9f, 0.20f, 8f, 14f);
        private readonly PidController _altitudePid = new PidController(1.1f, 0.06f, 0.10f, 12f, 8f);
        private readonly PidController _posXPid = new PidController(0.55f, 0.02f, 0.10f, 8f, 8f);
        private readonly PidController _posZPid = new PidController(0.55f, 0.02f, 0.10f, 8f, 8f);

        private readonly DroneInputState[] _inputBuffer = new DroneInputState[InputBufferSize];
        private readonly float[] _inputTimes = new float[InputBufferSize];
        private int _inputCursor;

        private DroneInputState _rawInput = DroneInputState.Neutral;
        private DroneInputState _effectiveInput = DroneInputState.Neutral;

        private float _holdAltitude;
        private float _groundAltitude;
        private float _thrustFraction;
        private float _crashDamage;
        private bool _armed;

        public FlightMode Mode { get; private set; }

        public AutoMode Auto
        {
            get { return _autopilot != null ? _autopilot.Mode : AutoMode.Off; }
        }

        public DroneState State { get; private set; }

        public Vector3 HomePoint { get; private set; }

        public DroneCameraRig Camera
        {
            get { return _camera; }
        }

        public DroneBattery Battery
        {
            get { return _battery; }
        }

        public DroneLink Link
        {
            get { return _link; }
        }

        /// <summary>Height above the terrain directly below, metres.</summary>
        public float AltitudeAgl
        {
            get { return Mathf.Max(0f, transform.position.y - _groundAltitude); }
        }

        /// <summary>Height above sea level, metres. Sea level is y = 0 in VTOL VR.</summary>
        public float AltitudeMsl
        {
            get { return transform.position.y; }
        }

        public float GroundSpeed
        {
            get
            {
                Vector3 v = _body != null ? _body.velocity : Vector3.zero;
                return new Vector2(v.x, v.z).magnitude;
            }
        }

        public float VerticalSpeed
        {
            get { return _body != null ? _body.velocity.y : 0f; }
        }

        public float Heading
        {
            get { return MathUtil.HeadingOf(transform.forward); }
        }

        public float DistanceToHome
        {
            get { return Vector3.Distance(transform.position, HomePoint); }
        }

        public bool IsAlive
        {
            get { return State == DroneState.Flying || State == DroneState.Landed; }
        }

        public string ModeLabel
        {
            get
            {
                if (Auto == AutoMode.ReturnHome)
                {
                    return "RTH";
                }

                if (Auto == AutoMode.Land)
                {
                    return "LAND";
                }

                switch (Mode)
                {
                    case FlightMode.Acro:
                        return "ACRO";
                    case FlightMode.Stabilised:
                        return "ANGLE";
                    case FlightMode.AltitudeHold:
                        return "ALT";
                    default:
                        return "POSN";
                }
            }
        }

        public void Initialise(
            Rigidbody body,
            DroneCameraRig camera,
            DroneBattery battery,
            DroneLink link,
            DroneAudio audio,
            DroneAutopilot autopilot,
            PropSpinner[] props)
        {
            _body = body;
            _camera = camera;
            _battery = battery;
            _link = link;
            _audio = audio;
            _autopilot = autopilot;
            _props = props;

            if (_autopilot != null)
            {
                _autopilot.Initialise(this);
            }

            Mode = FlightMode.AltitudeHold;
            State = DroneState.Stowed;

            // Stowed means parked: kinematic so it cannot fall through the world at
            // the origin while it waits to be deployed.
            _body.isKinematic = true;
            SetVisible(false);
        }

        // ---------------------------------------------------------------- lifecycle

        /// <summary>
        /// Places the drone in the world and arms it. <paramref name="initialVelocity"/>
        /// should match the launch platform, otherwise a drone dropped from a moving
        /// aircraft is instantly left behind at several hundred knots.
        /// </summary>
        public void Deploy(Vector3 position, Quaternion rotation, Vector3 initialVelocity, Vector3 homePoint)
        {
            transform.position = position;
            transform.rotation = rotation;

            _body.isKinematic = false;
            _body.velocity = initialVelocity;
            _body.angularVelocity = Vector3.zero;
            _body.WakeUp();

            HomePoint = homePoint;
            _holdAltitude = position.y;
            _crashDamage = 0f;
            _armed = true;
            State = DroneState.Flying;

            if (_battery != null)
            {
                _battery.Recharge();
            }

            if (_autopilot != null)
            {
                _autopilot.Cancel();
            }

            ResetControllers();
            SetVisible(true);

            ModLog.Info("Drone deployed at " + position.ToString("F0") + ".");
        }

        /// <summary>Removes the drone from the world without destroying it, ready to be deployed again.</summary>
        public void Stow()
        {
            _armed = false;
            State = DroneState.Stowed;

            if (_body != null)
            {
                _body.velocity = Vector3.zero;
                _body.angularVelocity = Vector3.zero;
                _body.isKinematic = true;
            }

            if (_camera != null)
            {
                _camera.SetActive(false);
            }

            SetVisible(false);
            ResetControllers();
        }

        private void SetVisible(bool visible)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = visible;
            }

            Light[] lights = GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                lights[i].enabled = visible;
            }

            Collider[] colliders = GetComponents<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = visible;
            }
        }

        private void ResetControllers()
        {
            _pitchPid.Reset();
            _rollPid.Reset();
            _yawPid.Reset();
            _climbPid.Reset();
            _altitudePid.Reset();
            _posXPid.Reset();
            _posZPid.Reset();

            for (int i = 0; i < InputBufferSize; i++)
            {
                _inputBuffer[i] = DroneInputState.Neutral;
                _inputTimes[i] = -999f;
            }
        }

        /// <summary>Forces a flight mode, e.g. when the goggles go up and the drone must park itself.</summary>
        public void SetFlightMode(FlightMode mode)
        {
            if (Mode == mode)
            {
                return;
            }

            Mode = mode;
            _holdAltitude = transform.position.y;
            _climbPid.Reset();
            _altitudePid.Reset();
            _posXPid.Reset();
            _posZPid.Reset();
        }

        public void CycleFlightMode()
        {
            Mode = (FlightMode)(((int)Mode + 1) % 4);
            _holdAltitude = transform.position.y;
            _climbPid.Reset();
            _altitudePid.Reset();
            _posXPid.Reset();
            _posZPid.Reset();
            ModLog.Trace("Flight mode: " + ModeLabel);
        }

        public void ToggleReturnHome()
        {
            if (_autopilot == null)
            {
                return;
            }

            if (_autopilot.Mode == AutoMode.Off)
            {
                _autopilot.Begin(AutoMode.ReturnHome);
            }
            else
            {
                _autopilot.Cancel();
            }
        }

        // -------------------------------------------------------------------- input

        /// <summary>Feeds one frame of pilot demand. Stored with a timestamp so link latency can be applied.</summary>
        public void SetInput(DroneInputState state)
        {
            _rawInput = state;

            _inputCursor = (_inputCursor + 1) % InputBufferSize;
            _inputBuffer[_inputCursor] = state;
            _inputTimes[_inputCursor] = Time.time;
        }

        /// <summary>
        /// Returns the command that has actually arrived at the drone, which is the
        /// one the pilot gave one link-latency ago. At a strong signal this is
        /// imperceptible; at the edge of range it is very much not.
        /// </summary>
        private DroneInputState GetDelayedInput()
        {
            if (_link == null)
            {
                return _rawInput;
            }

            float wanted = Time.time - _link.Latency;

            DroneInputState best = _rawInput;
            float bestDelta = float.MaxValue;

            for (int i = 0; i < InputBufferSize; i++)
            {
                if (_inputTimes[i] < 0f)
                {
                    continue;
                }

                float delta = Mathf.Abs(_inputTimes[i] - wanted);
                if (delta < bestDelta)
                {
                    bestDelta = delta;
                    best = _inputBuffer[i];
                }
            }

            return best;
        }

        // ------------------------------------------------------------------ physics

        private void FixedUpdate()
        {
            if (!_armed || _body == null)
            {
                return;
            }

            float dt = Time.fixedDeltaTime;

            UpdateGroundAltitude();

            DroneInputState input = GetDelayedInput();

            // Failsafes override the pilot. Link loss and a flat battery both hand
            // the drone to the autopilot, which is what a real quad does.
            if (_autopilot != null)
            {
                _autopilot.EvaluateFailsafes();
                if (_autopilot.Mode != AutoMode.Off)
                {
                    input = _autopilot.ComputeInput(input, dt);
                }
            }

            // A flat battery is not a failsafe, it is the end of the flight.
            bool powered = _battery == null || !_battery.IsFlat;
            if (!powered)
            {
                input = DroneInputState.Neutral;
            }

            _effectiveInput = input;

            if (State == DroneState.Crashed || !powered)
            {
                ApplyAerodynamics(dt);
                UpdateProps(0f);
                if (_audio != null)
                {
                    _audio.UpdateMotors(0f, false);
                }

                return;
            }

            ApplyAttitudeControl(input, dt);
            ApplyThrust(input, dt);
            ApplyAerodynamics(dt);
            ClampSpeed();
            UpdateGroundContact(input);

            if (_battery != null)
            {
                _battery.Consume(_thrustFraction, dt);
            }

            UpdateProps(_thrustFraction);
            if (_audio != null)
            {
                _audio.UpdateMotors(_thrustFraction, true);
            }
        }

        private void UpdateGroundAltitude()
        {
            // A 6 km ray every physics step is wasteful; a third of that rate is
            // still under 20 ms of lag on the altitude readout.
            if (++_groundCheckCounter % 3 != 0)
            {
                return;
            }

            RaycastHit[] hits = Physics.RaycastAll(transform.position + Vector3.up * 2f, Vector3.down, 6000f, ~0, QueryTriggerInteraction.Ignore);

            float best = 0f;           // Sea level, the floor everywhere in VTOL VR.
            float bestDistance = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform != null && hits[i].transform.IsChildOf(transform))
                {
                    continue;
                }

                if (hits[i].distance < bestDistance)
                {
                    bestDistance = hits[i].distance;
                    best = hits[i].point.y;
                }
            }

            _groundAltitude = best;
        }

        private void ApplyAttitudeControl(DroneInputState input, float dt)
        {
            Vector3 localAngularVelocity = transform.InverseTransformDirection(_body.angularVelocity);

            // Body-axis rates in degrees per second, in pilot-facing sign conventions.
            float pitchRate = -localAngularVelocity.x * Mathf.Rad2Deg;   // positive = nose up
            float rollRate = -localAngularVelocity.z * Mathf.Rad2Deg;    // positive = roll right
            float yawRate = localAngularVelocity.y * Mathf.Rad2Deg;      // positive = yaw right

            float pitchCommand;
            float rollCommand;

            if (Mode == FlightMode.Acro)
            {
                // Rate mode: the sticks are a rate demand and nothing self-levels.
                float targetPitchRate = input.Pitch * 360f;
                float targetRollRate = input.Roll * 420f;
                pitchCommand = _pitchPid.Update(targetPitchRate, pitchRate, dt) * 0.35f;
                rollCommand = _rollPid.Update(targetRollRate, rollRate, dt) * 0.35f;
            }
            else
            {
                float targetPitch = input.Pitch * ModConfig.MaxTiltAngle;
                float targetRoll = input.Roll * ModConfig.MaxTiltAngle;

                // Position hold leans the drone into the wind to stay put.
                if (Mode == FlightMode.PositionHold && input.IsCentred(0.05f))
                {
                    Vector3 correction = ComputePositionHoldTilt(dt);
                    targetPitch += correction.x;
                    targetRoll += correction.y;
                }

                targetPitch = Mathf.Clamp(targetPitch, -ModConfig.MaxTiltAngle, ModConfig.MaxTiltAngle);
                targetRoll = Mathf.Clamp(targetRoll, -ModConfig.MaxTiltAngle, ModConfig.MaxTiltAngle);

                float pitchAngle = MathUtil.PitchOf(transform.forward);
                float rollAngle = -Mathf.Asin(Mathf.Clamp(transform.right.y, -1f, 1f)) * Mathf.Rad2Deg;

                pitchCommand = _pitchPid.Update(targetPitch, pitchAngle, dt);
                rollCommand = _rollPid.Update(targetRoll, rollAngle, dt);
            }

            float yawCommand = _yawPid.Update(input.Yaw * ModConfig.MaxYawRate, yawRate, dt);

            // Torque about local X pitches the nose down, about Z rolls left, hence the negations.
            Vector3 torque = new Vector3(-pitchCommand, yawCommand, -rollCommand) * MaxAngularAccel;

            // Airframe damping, so the drone settles instead of ringing.
            torque -= localAngularVelocity * AngularDamping;

            _body.AddRelativeTorque(torque, ForceMode.Acceleration);
        }

        /// <summary>
        /// Returns the extra pitch/roll (x = pitch, y = roll, degrees) needed to null
        /// the drone's horizontal velocity for position hold.
        /// </summary>
        private Vector3 ComputePositionHoldTilt(float dt)
        {
            Vector3 velocity = _body.velocity - GameBridge.GetWind();
            Vector3 local = transform.InverseTransformDirection(new Vector3(velocity.x, 0f, velocity.z));

            float pitch = _posZPid.Update(0f, local.z, dt);
            float roll = _posXPid.Update(0f, local.x, dt);

            return new Vector3(
                Mathf.Clamp(pitch, -18f, 18f),
                Mathf.Clamp(roll, -18f, 18f),
                0f);
        }

        private void ApplyThrust(DroneInputState input, float dt)
        {
            float gravity = Mathf.Abs(Physics.gravity.y);
            float maxAccel = gravity * Mathf.Max(1.05f, ModConfig.ThrustToWeight);

            // Tilting the airframe tips the thrust vector away from vertical, so more
            // total thrust is needed to hold height. This is why a hard turn sinks.
            float upness = Vector3.Dot(transform.up, Vector3.up);
            float tiltCompensation = 1f / Mathf.Clamp(upness, 0.35f, 1f);

            float accel;

            if (Mode == FlightMode.Acro || Mode == FlightMode.Stabilised)
            {
                accel = input.ThrottleAbsolute * maxAccel;
            }
            else
            {
                float stick = input.ThrottleStick;
                float climbTarget;

                if (Mathf.Abs(stick) > 0.08f)
                {
                    climbTarget = stick * ModConfig.MaxClimbRate;
                    _holdAltitude = transform.position.y;
                    _altitudePid.Reset();
                }
                else
                {
                    climbTarget = Mathf.Clamp(
                        _altitudePid.Update(_holdAltitude, transform.position.y, dt),
                        -ModConfig.MaxClimbRate,
                        ModConfig.MaxClimbRate);
                }

                float hover = gravity * tiltCompensation;
                accel = hover + _climbPid.Update(climbTarget, _body.velocity.y, dt);
            }

            accel = Mathf.Clamp(accel, 0f, maxAccel);

            // Ground effect: the rotor wash reflecting off the surface adds lift
            // within roughly one rotor span of the ground.
            float agl = AltitudeAgl;
            if (agl < RotorSpan * 2f)
            {
                float proximity = 1f - Mathf.Clamp01(agl / (RotorSpan * 2f));
                accel *= 1f + 0.22f * proximity * proximity;
            }

            // A sagging battery cannot deliver full current.
            if (_battery != null && _battery.Charge < 0.12f)
            {
                accel *= Mathf.Lerp(0.55f, 1f, _battery.Charge / 0.12f);
            }

            _thrustFraction = accel / Mathf.Max(0.01f, gravity);
            _body.AddRelativeForce(Vector3.up * accel, ForceMode.Acceleration);
        }

        private void ApplyAerodynamics(float dt)
        {
            Vector3 relative = _body.velocity - GameBridge.GetWind();
            Vector3 local = transform.InverseTransformDirection(relative);
            Vector3 drag = -Vector3.Scale(MathUtil.SignedSquare(local), DragCoefficients);
            _body.AddRelativeForce(drag, ForceMode.Acceleration);
        }

        private void ClampSpeed()
        {
            float limit = ModConfig.MaxHorizontalSpeed * 1.25f;
            Vector3 velocity = _body.velocity;
            Vector2 horizontal = new Vector2(velocity.x, velocity.z);

            if (horizontal.magnitude > limit)
            {
                horizontal = horizontal.normalized * limit;
                _body.velocity = new Vector3(horizontal.x, velocity.y, horizontal.y);
            }
        }

        private void UpdateGroundContact(DroneInputState input)
        {
            bool nearGround = AltitudeAgl < 0.12f;
            bool slow = _body.velocity.magnitude < 0.6f;

            if (nearGround && slow && _thrustFraction < 0.6f)
            {
                State = DroneState.Landed;
            }
            else if (State == DroneState.Landed && (!nearGround || _thrustFraction > 0.85f))
            {
                State = DroneState.Flying;
            }

            // VTOL VR's sea is at y = 0 and a quad does not float.
            if (transform.position.y < 0f && _groundAltitude <= 0.01f)
            {
                Crash("ditched in the sea");
            }
        }

        private void UpdateProps(float load)
        {
            if (_props == null)
            {
                return;
            }

            float normalised = Mathf.Clamp01(load / Mathf.Max(1.05f, ModConfig.ThrustToWeight));
            for (int i = 0; i < _props.Length; i++)
            {
                if (_props[i] != null)
                {
                    _props[i].Speed = normalised;
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_armed || State == DroneState.Crashed)
            {
                return;
            }

            float impact = collision.relativeVelocity.magnitude;
            if (impact < 2.5f)
            {
                return;
            }

            _crashDamage += impact;
            ModLog.Trace("Drone impact at " + impact.ToString("F1") + " m/s (cumulative " + _crashDamage.ToString("F1") + ").");

            if (impact > CrashSpeed || _crashDamage > CrashSpeed * 2.2f)
            {
                Crash("hit " + (collision.collider != null ? collision.collider.name : "terrain") + " at " + Mathf.RoundToInt(impact) + " m/s");
            }
        }

        private void Crash(string reason)
        {
            if (State == DroneState.Crashed)
            {
                return;
            }

            State = DroneState.Crashed;
            _thrustFraction = 0f;

            if (_autopilot != null)
            {
                _autopilot.Cancel();
            }

            UpdateProps(0f);
            ModLog.Info("Drone lost: " + reason + ".");
        }

        /// <summary>The demand actually being flown, after latency and any autopilot override.</summary>
        public DroneInputState EffectiveInput
        {
            get { return _effectiveInput; }
        }

        public float ThrustFraction
        {
            get { return _thrustFraction; }
        }
    }
}
