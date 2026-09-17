using UnityEngine;
using DroneMod.Util;

namespace DroneMod.Config
{
    /// <summary>
    /// Every tunable in one place. Values are persisted through PlayerPrefs so the
    /// in-game settings menu survives a restart; the flight-model constants are
    /// deliberately exposed too, because "what does a good quad feel like" is
    /// personal and everyone retunes it.
    /// </summary>
    internal static class ModConfig
    {
        private const string KeyPrefix = "FPVDrone.";

        // ---------------------------------------------------------------- keys

        public static KeyCode DeployKey = KeyCode.F8;
        public static KeyCode GogglesKey = KeyCode.F9;
        public static KeyCode CameraModeKey = KeyCode.F10;
        public static KeyCode FlightModeKey = KeyCode.F11;
        public static KeyCode ReturnHomeKey = KeyCode.F12;
        public static KeyCode RecenterGimbalKey = KeyCode.Home;

        // Desktop fallback stick keys, for testing outside a headset.
        public static KeyCode ThrottleUpKey = KeyCode.W;
        public static KeyCode ThrottleDownKey = KeyCode.S;
        public static KeyCode YawLeftKey = KeyCode.A;
        public static KeyCode YawRightKey = KeyCode.D;
        public static KeyCode PitchForwardKey = KeyCode.UpArrow;
        public static KeyCode PitchBackKey = KeyCode.DownArrow;
        public static KeyCode RollLeftKey = KeyCode.LeftArrow;
        public static KeyCode RollRightKey = KeyCode.RightArrow;
        public static KeyCode GimbalUpKey = KeyCode.Q;
        public static KeyCode GimbalDownKey = KeyCode.E;
        public static KeyCode ZoomInKey = KeyCode.PageUp;
        public static KeyCode ZoomOutKey = KeyCode.PageDown;

        // ------------------------------------------------------------ handling

        /// <summary>Mode 2 (throttle left) is the near-universal default outside Japan.</summary>
        public static bool Mode2Sticks = true;

        public static float StickDeadzone = 0.12f;
        public static float StickExpo = 0.45f;

        /// <summary>Maximum commanded bank/pitch angle in stabilised mode, degrees.</summary>
        public static float MaxTiltAngle = 38f;

        /// <summary>Maximum commanded yaw rate, degrees per second.</summary>
        public static float MaxYawRate = 180f;

        /// <summary>Peak thrust expressed as a multiple of hover thrust. 2.2 is a sporty 5" quad.</summary>
        public static float ThrustToWeight = 2.6f;

        public static float MaxClimbRate = 8f;
        public static float MaxHorizontalSpeed = 38f;

        // ------------------------------------------------------------- systems

        /// <summary>Battery endurance at hover, in seconds.</summary>
        public static float BatterySeconds = 600f;

        /// <summary>Video/control link range in metres before the picture starts to break up.</summary>
        public static float LinkRange = 4000f;

        /// <summary>Refuse to launch above this airspeed (m/s). A 900 g quad does not survive a 300 kt launch.</summary>
        public static float MaxLaunchAirspeed = 60f;

        public static bool AllowHighSpeedLaunch = false;

        /// <summary>Engage the aircraft's autopilot, if one can be found, when the goggles come down.</summary>
        public static bool AutopilotOnGoggles = true;

        /// <summary>Return home automatically on link loss or a flat battery.</summary>
        public static bool FailsafeReturnHome = true;

        /// <summary>
        /// Drop the drone into position hold when the goggles go up. Without this a
        /// drone left in acro while you look at your instruments simply flies away.
        /// </summary>
        public static bool HoldOnGogglesUp = true;

        // ---------------------------------------------------------------- view

        /// <summary>Distance from the eyes to the goggle screen, metres.</summary>
        public static float ScreenDistance = 0.145f;

        public static float ScreenWidth = 0.19f;
        public static float ScreenHeight = 0.107f;
        public static float ScreenCurvature = 0.35f;

        /// <summary>Render texture edge length in pixels. Lower this first if the feed costs you frames.</summary>
        public static int FeedResolution = 1024;

        public static bool ShowOsd = true;
        public static bool EnableStatic = true;

        /// <summary>Camera field of view when not zoomed, degrees. FPV cameras are very wide.</summary>
        public static float CameraFov = 95f;
        public static float CameraMinFov = 18f;

        /// <summary>Fixed upward tilt of the FPV camera, as every racing quad has.</summary>
        public static float CameraTilt = 25f;

        /// <summary>
        /// Hold the camera against the world horizon instead of bolting it to the
        /// airframe. Off gives the classic hard-mounted FPV picture that rolls with
        /// the quad; on gives a gimbal-smooth reconnaissance picture.
        /// </summary>
        public static bool StabiliseGimbal = true;

        public static bool VerboseLogging = false;

        // ------------------------------------------------------------ persistence

        public static void Load()
        {
            Mode2Sticks = GetBool("Mode2Sticks", Mode2Sticks);
            StickDeadzone = GetFloat("StickDeadzone", StickDeadzone);
            StickExpo = GetFloat("StickExpo", StickExpo);
            MaxTiltAngle = GetFloat("MaxTiltAngle", MaxTiltAngle);
            MaxYawRate = GetFloat("MaxYawRate", MaxYawRate);
            ThrustToWeight = GetFloat("ThrustToWeight", ThrustToWeight);
            MaxClimbRate = GetFloat("MaxClimbRate", MaxClimbRate);
            MaxHorizontalSpeed = GetFloat("MaxHorizontalSpeed", MaxHorizontalSpeed);

            BatterySeconds = GetFloat("BatterySeconds", BatterySeconds);
            LinkRange = GetFloat("LinkRange", LinkRange);
            MaxLaunchAirspeed = GetFloat("MaxLaunchAirspeed", MaxLaunchAirspeed);
            AllowHighSpeedLaunch = GetBool("AllowHighSpeedLaunch", AllowHighSpeedLaunch);
            AutopilotOnGoggles = GetBool("AutopilotOnGoggles", AutopilotOnGoggles);
            FailsafeReturnHome = GetBool("FailsafeReturnHome", FailsafeReturnHome);
            HoldOnGogglesUp = GetBool("HoldOnGogglesUp", HoldOnGogglesUp);

            ScreenDistance = GetFloat("ScreenDistance", ScreenDistance);
            ScreenWidth = GetFloat("ScreenWidth", ScreenWidth);
            ScreenHeight = GetFloat("ScreenHeight", ScreenHeight);
            ScreenCurvature = GetFloat("ScreenCurvature", ScreenCurvature);
            FeedResolution = Mathf.Clamp(Mathf.RoundToInt(GetFloat("FeedResolution", FeedResolution)), 256, 2048);
            ShowOsd = GetBool("ShowOsd", ShowOsd);
            EnableStatic = GetBool("EnableStatic", EnableStatic);
            CameraFov = GetFloat("CameraFov", CameraFov);
            CameraMinFov = GetFloat("CameraMinFov", CameraMinFov);
            CameraTilt = GetFloat("CameraTilt", CameraTilt);
            StabiliseGimbal = GetBool("StabiliseGimbal", StabiliseGimbal);

            VerboseLogging = GetBool("VerboseLogging", VerboseLogging);
            ModLog.Verbose = VerboseLogging;

            ModLog.Info("Configuration loaded.");
        }

        public static void Save()
        {
            SetBool("Mode2Sticks", Mode2Sticks);
            SetFloat("StickDeadzone", StickDeadzone);
            SetFloat("StickExpo", StickExpo);
            SetFloat("MaxTiltAngle", MaxTiltAngle);
            SetFloat("MaxYawRate", MaxYawRate);
            SetFloat("ThrustToWeight", ThrustToWeight);
            SetFloat("MaxClimbRate", MaxClimbRate);
            SetFloat("MaxHorizontalSpeed", MaxHorizontalSpeed);

            SetFloat("BatterySeconds", BatterySeconds);
            SetFloat("LinkRange", LinkRange);
            SetFloat("MaxLaunchAirspeed", MaxLaunchAirspeed);
            SetBool("AllowHighSpeedLaunch", AllowHighSpeedLaunch);
            SetBool("AutopilotOnGoggles", AutopilotOnGoggles);
            SetBool("FailsafeReturnHome", FailsafeReturnHome);
            SetBool("HoldOnGogglesUp", HoldOnGogglesUp);

            SetFloat("ScreenDistance", ScreenDistance);
            SetFloat("ScreenWidth", ScreenWidth);
            SetFloat("ScreenHeight", ScreenHeight);
            SetFloat("ScreenCurvature", ScreenCurvature);
            SetFloat("FeedResolution", FeedResolution);
            SetBool("ShowOsd", ShowOsd);
            SetBool("EnableStatic", EnableStatic);
            SetFloat("CameraFov", CameraFov);
            SetFloat("CameraMinFov", CameraMinFov);
            SetFloat("CameraTilt", CameraTilt);
            SetBool("StabiliseGimbal", StabiliseGimbal);

            SetBool("VerboseLogging", VerboseLogging);

            PlayerPrefs.Save();
            ModLog.Trace("Configuration saved.");
        }

        private static float GetFloat(string key, float fallback)
        {
            return PlayerPrefs.GetFloat(KeyPrefix + key, fallback);
        }

        private static void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(KeyPrefix + key, value);
        }

        private static bool GetBool(string key, bool fallback)
        {
            return PlayerPrefs.GetInt(KeyPrefix + key, fallback ? 1 : 0) != 0;
        }

        private static void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(KeyPrefix + key, value ? 1 : 0);
        }
    }
}
