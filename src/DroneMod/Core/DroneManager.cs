using UnityEngine;
using DroneMod.Config;
using DroneMod.Drone;
using DroneMod.Input;
using DroneMod.Util;
using DroneMod.View;

namespace DroneMod.Core
{
    /// <summary>
    /// Owns the drone, the goggles and the wiring between them. One of these lives
    /// for the whole session; the drone and goggles themselves are created when a
    /// flight scene comes up and destroyed when it goes away.
    /// </summary>
    internal sealed class DroneManager : MonoBehaviour
    {
        /// <summary>How far behind and below the aircraft the drone is released.</summary>
        private static readonly Vector3 LaunchOffset = new Vector3(0f, -4f, -6f);

        private static DroneManager _instance;

        private DroneController _drone;
        private FpvGoggles _goggles;
        private HeadNotifier _notifier;
        private readonly DroneInput _input = new DroneInput();

        private bool _autopilotEngagedByUs;
        private bool _sceneReady;
        private float _nextSetupAttempt;
        private float _nextSceneProbe;

        public static DroneManager Instance
        {
            get { return _instance; }
        }

        public static DroneManager Create()
        {
            if (_instance != null)
            {
                return _instance;
            }

            GameObject host = new GameObject("FPVDroneManager");
            DontDestroyOnLoad(host);
            _instance = host.AddComponent<DroneManager>();
            ModLog.Info("Manager created.");
            return _instance;
        }

        // ---------------------------------------------------------------- lifecycle

        /// <summary>Called by the loader entry point whenever the scene changes.</summary>
        public void OnSceneChanged(bool isFlightScene)
        {
            GameBridge.Invalidate();
            TearDown();

            _sceneReady = isFlightScene;
            _nextSetupAttempt = 0f;

            ModLog.Trace("Scene changed; flight scene = " + isFlightScene + ".");
        }

        private void TearDown()
        {
            if (_drone != null)
            {
                Destroy(_drone.gameObject);
                _drone = null;
            }

            if (_goggles != null)
            {
                Destroy(_goggles.gameObject);
                _goggles = null;
            }

            if (_notifier != null)
            {
                Destroy(_notifier.gameObject);
                _notifier = null;
            }

            _autopilotEngagedByUs = false;
        }

        /// <summary>
        /// Builds the goggles once the head transform exists. The flight scene loads
        /// over several frames, so this retries rather than assuming the first
        /// attempt will find anything.
        /// </summary>
        private bool TryBuildRig()
        {
            if (_goggles != null)
            {
                return true;
            }

            if (Time.unscaledTime < _nextSetupAttempt)
            {
                return false;
            }

            _nextSetupAttempt = Time.unscaledTime + 1f;

            Transform head = GameBridge.Head;
            if (head == null)
            {
                return false;
            }

            GameObject gogglesObject = new GameObject("FpvGoggles");
            _goggles = gogglesObject.AddComponent<FpvGoggles>();
            _goggles.Build(head);

            GameObject notifierObject = new GameObject("FpvNotifier");
            _notifier = notifierObject.AddComponent<HeadNotifier>();
            _notifier.Build(head);

            ModLog.Info("Goggles ready. Press " + ModConfig.DeployKey + " to deploy the drone.");
            return true;
        }

        // -------------------------------------------------------------------- frame

        private void Update()
        {
            if (!_sceneReady)
            {
                // The loader tells us about scene changes, but not every loader build
                // fires that event reliably, so we also sniff for a flyable scene.
                if (Time.unscaledTime < _nextSceneProbe)
                {
                    return;
                }

                _nextSceneProbe = Time.unscaledTime + 2f;
                GameBridge.Invalidate();

                if (!GameBridge.InFlightScene)
                {
                    return;
                }

                _sceneReady = true;
                ModLog.Trace("Flight scene detected by probe.");
            }

            if (!TryBuildRig())
            {
                return;
            }

            HandleHotkeys();

            float dt = Time.deltaTime;
            DroneInputState polled = _input.Poll(dt);

            if (_drone != null)
            {
                if (_drone.IsAlive)
                {
                    // The drone only answers the sticks while you are looking through
                    // it. With the visor up your hands belong to the aircraft, which
                    // is the whole point of the goggles being a physical object.
                    DroneInputState state = polled;
                    if (!_goggles.IsDown)
                    {
                        state = DroneInputState.Neutral;
                        state.ThrottleAbsolute = polled.ThrottleAbsolute;
                    }

                    _drone.SetInput(state);
                }

                if (_drone.Camera != null)
                {
                    // No point paying for a camera pass from a drone that is in its case.
                    _drone.Camera.SetActive(_goggles.IsDown && _drone.State != DroneState.Stowed);
                    _drone.Camera.UpdateGimbal(polled.GimbalPitch, polled.GimbalYaw, polled.Zoom, dt);
                }
            }

            if (_notifier != null)
            {
                _notifier.SetVisible(!_goggles.IsDown);
            }

            _goggles.Refresh(_drone);
        }

        private void HandleHotkeys()
        {
            if (UnityEngine.Input.GetKeyDown(ModConfig.DeployKey))
            {
                ToggleDeployment();
            }

            if (UnityEngine.Input.GetKeyDown(ModConfig.GogglesKey))
            {
                ToggleGoggles();
            }

            if (_drone == null)
            {
                return;
            }

            if (UnityEngine.Input.GetKeyDown(ModConfig.FlightModeKey))
            {
                _drone.CycleFlightMode();
                Notify(_drone.ModeLabel);
            }

            if (UnityEngine.Input.GetKeyDown(ModConfig.ReturnHomeKey))
            {
                _drone.ToggleReturnHome();
                Notify(_drone.Auto == AutoMode.Off ? "RTH CANCELLED" : "RETURNING HOME");
            }

            if (_drone.Camera != null)
            {
                if (UnityEngine.Input.GetKeyDown(ModConfig.CameraModeKey))
                {
                    _drone.Camera.CycleMode();
                    Notify("CAMERA " + _drone.Camera.ModeLabel);
                }

                if (UnityEngine.Input.GetKeyDown(ModConfig.RecenterGimbalKey))
                {
                    _drone.Camera.Recentre();
                }
            }
        }

        // ------------------------------------------------------------- deployment

        public void ToggleDeployment()
        {
            if (_drone == null || _drone.State == DroneState.Stowed)
            {
                Deploy();
            }
            else
            {
                Recover();
            }
        }

        private void Deploy()
        {
            GameObject vehicle = GameBridge.PlayerVehicle;
            if (vehicle == null)
            {
                Notify("NO AIRCRAFT FOUND");
                return;
            }

            Vector3 vehicleVelocity = GameBridge.PlayerVelocity;
            float airspeed = vehicleVelocity.magnitude;

            if (airspeed > ModConfig.MaxLaunchAirspeed && !ModConfig.AllowHighSpeedLaunch)
            {
                Notify("AIRSPEED TOO HIGH  " + Mathf.RoundToInt(airspeed) + " m/s");
                return;
            }

            if (_drone == null)
            {
                _drone = DroneBuilder.Build();
            }

            Transform vehicleTransform = vehicle.transform;
            bool onGround = airspeed < 3f;

            Vector3 position;
            Quaternion rotation = Quaternion.LookRotation(
                Vector3.ProjectOnPlane(vehicleTransform.forward, Vector3.up).normalized,
                Vector3.up);

            if (onGround)
            {
                // Sitting on the deck: put the drone on the ground off the right wing.
                Vector3 beside = vehicleTransform.position + vehicleTransform.right * 7f;
                position = SnapToGround(beside) + Vector3.up * 0.15f;
            }
            else
            {
                position = vehicleTransform.position
                    + vehicleTransform.right * LaunchOffset.x
                    + vehicleTransform.up * LaunchOffset.y
                    + vehicleTransform.forward * LaunchOffset.z;
            }

            // Matching the launch platform's velocity is what stops the drone from
            // being torn off the back of a moving aircraft.
            _drone.Deploy(position, rotation, onGround ? Vector3.zero : vehicleVelocity, position);

            // After Deploy, not before: Physics.IgnoreCollision refuses to pair
            // colliders that are disabled, and the drone's are off while stowed.
            IgnoreCollisionsWith(vehicle);

            if (_goggles != null)
            {
                _goggles.BindFeed(_drone.Camera != null ? _drone.Camera.Feed : null);
                _goggles.Osd.ResetFlightTimer();
            }

            _input.ResetThrottle(onGround ? 0f : 0.5f);

            Notify(onGround ? "DRONE DEPLOYED" : "DRONE RELEASED");
        }

        private void Recover()
        {
            if (_drone == null)
            {
                return;
            }

            bool airborne = _drone.State == DroneState.Flying && _drone.AltitudeAgl > 1.5f;

            _drone.Stow();

            if (_goggles != null)
            {
                _goggles.BindFeed(null);
            }

            Notify(airborne ? "DRONE RECALLED IN FLIGHT" : "DRONE RECOVERED");
        }

        private static Vector3 SnapToGround(Vector3 position)
        {
            RaycastHit hit;
            if (Physics.Raycast(position + Vector3.up * 200f, Vector3.down, out hit, 2000f, ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.point;
            }

            return new Vector3(position.x, Mathf.Max(0f, position.y), position.z);
        }

        /// <summary>
        /// Stops the drone from colliding with the aircraft it was just launched from,
        /// which otherwise happens immediately and destroys both the mood and the drone.
        /// </summary>
        private void IgnoreCollisionsWith(GameObject vehicle)
        {
            if (_drone == null || vehicle == null)
            {
                return;
            }

            Collider[] droneColliders = _drone.GetComponents<Collider>();
            Collider[] vehicleColliders = vehicle.GetComponentsInChildren<Collider>();

            for (int i = 0; i < droneColliders.Length; i++)
            {
                for (int j = 0; j < vehicleColliders.Length; j++)
                {
                    if (droneColliders[i] == null || vehicleColliders[j] == null)
                    {
                        continue;
                    }

                    Physics.IgnoreCollision(droneColliders[i], vehicleColliders[j], true);
                }
            }

            ModLog.Trace("Ignoring collisions against " + vehicleColliders.Length + " aircraft colliders.");
        }

        // ---------------------------------------------------------------- goggles

        public void ToggleGoggles()
        {
            if (_goggles == null)
            {
                return;
            }

            _goggles.Toggle();

            if (_goggles.IsDown)
            {
                // Flying a jet by feel while staring at a drone feed ends one way.
                if (ModConfig.AutopilotOnGoggles)
                {
                    if (GameBridge.TrySetAutopilot(true))
                    {
                        _autopilotEngagedByUs = true;
                        Notify("GOGGLES DOWN  AUTOPILOT ENGAGED");
                    }
                    else
                    {
                        Notify("GOGGLES DOWN  NO AUTOPILOT - MIND YOUR AIRCRAFT");
                    }
                }
                else
                {
                    Notify("GOGGLES DOWN");
                }
            }
            else
            {
                if (_autopilotEngagedByUs)
                {
                    GameBridge.TrySetAutopilot(false);
                    _autopilotEngagedByUs = false;
                }

                // Park the drone rather than leaving it flying itself into a hill
                // while the pilot is busy with the aircraft.
                if (ModConfig.HoldOnGogglesUp && _drone != null && _drone.State == DroneState.Flying
                    && (_drone.Mode == FlightMode.Acro || _drone.Mode == FlightMode.Stabilised))
                {
                    _drone.SetFlightMode(FlightMode.PositionHold);
                    Notify("GOGGLES UP  DRONE HOLDING");
                }
                else
                {
                    Notify("GOGGLES UP");
                }
            }
        }

        /// <summary>
        /// Routes a message to whichever display the pilot can actually see: the OSD
        /// when the visor is down, the head-mounted line of text when it is up.
        /// </summary>
        private void Notify(string message)
        {
            bool gogglesDown = _goggles != null && _goggles.IsDown;

            if (gogglesDown)
            {
                _goggles.Osd.ShowWarning(message, 2.5f);
                ModLog.Info(message);
            }
            else if (_notifier != null)
            {
                _notifier.Show(message, 3f);
            }
            else
            {
                ModLog.Info(message);
            }
        }

        /// <summary>Re-applies settings that need more than a field write, after the settings menu changes them.</summary>
        public void ApplySettings()
        {
            ModLog.Verbose = ModConfig.VerboseLogging;

            if (_drone != null && _drone.Camera != null)
            {
                _drone.Camera.ReapplyResolution();
                if (_goggles != null)
                {
                    _goggles.BindFeed(_drone.Camera.Feed);
                }
            }

            RebuildGogglesIfGeometryChanged();
        }

        /// <summary>
        /// The screen mesh is generated once at build time, so changing its size,
        /// curvature or distance means building a new pair of goggles. The visor
        /// keeps its up/down position across the swap.
        /// </summary>
        private void RebuildGogglesIfGeometryChanged()
        {
            if (_goggles == null)
            {
                return;
            }

            if (_goggles.BuiltGeometry.Matches(ScreenGeometry.FromConfig()))
            {
                return;
            }

            bool wasDown = _goggles.IsDown;

            Destroy(_goggles.gameObject);
            _goggles = null;
            _nextSetupAttempt = 0f;

            if (!TryBuildRig())
            {
                return;
            }

            _goggles.SetDown(wasDown);
            if (_drone != null && _drone.Camera != null)
            {
                _goggles.BindFeed(_drone.Camera.Feed);
            }

            ModLog.Trace("Goggles rebuilt after a geometry change.");
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
