using System;
using ModLoader;
using UnityEngine;
using DroneMod.Config;
using DroneMod.Util;

namespace DroneMod
{
    /// <summary>
    /// The VTOL VR Mod Loader entry point.
    ///
    /// This is the only file in the project that references the loader, and it is
    /// deliberately thin: it loads settings, starts <see cref="Bootstrap"/>, and
    /// forwards scene changes. If a loader update changes any of these signatures,
    /// this file is the only thing that needs fixing -- see docs/COMPATIBILITY.md.
    /// </summary>
    public class Main : VTOLMOD
    {
        public override void ModLoaded()
        {
            base.ModLoaded();

            try
            {
                Bootstrap.Initialise();
            }
            catch (Exception e)
            {
                ModLog.Exception(e, "Bootstrap failed");
            }

            try
            {
                VTOLAPI.SceneLoaded += OnSceneLoaded;
            }
            catch (Exception e)
            {
                ModLog.Exception(e, "Could not subscribe to scene events; falling back to scene probing");
            }

            try
            {
                BuildSettingsMenu();
            }
            catch (Exception e)
            {
                ModLog.Exception(e, "Could not build the settings menu");
            }
        }

        private void OnSceneLoaded(VTOLScenes scene)
        {
            Bootstrap.NotifySceneChanged(scene.ToString());
        }

        /// <summary>
        /// Builds the in-game settings page. Every setter writes the value and then
        /// asks the mod to re-apply anything that needs more than a field write.
        /// </summary>
        private void BuildSettingsMenu()
        {
            Settings settings = new Settings(this);

            settings.CreateCustomLabel("Sticks");
            settings.CreateBoolSetting("Mode 2 (throttle on the left stick)", OnMode2Changed, ModConfig.Mode2Sticks);
            settings.CreateFloatSetting("Stick deadzone", OnDeadzoneChanged, ModConfig.StickDeadzone, 0f, 0.4f);
            settings.CreateFloatSetting("Stick expo", OnExpoChanged, ModConfig.StickExpo, 0f, 0.9f);

            settings.CreateCustomLabel("Handling");
            settings.CreateFloatSetting("Max tilt angle (deg)", OnTiltChanged, ModConfig.MaxTiltAngle, 10f, 70f);
            settings.CreateFloatSetting("Max yaw rate (deg/s)", OnYawRateChanged, ModConfig.MaxYawRate, 60f, 500f);
            settings.CreateFloatSetting("Thrust to weight", OnThrustChanged, ModConfig.ThrustToWeight, 1.2f, 6f);
            settings.CreateFloatSetting("Max climb rate (m/s)", OnClimbChanged, ModConfig.MaxClimbRate, 2f, 25f);
            settings.CreateFloatSetting("Max speed (m/s)", OnSpeedChanged, ModConfig.MaxHorizontalSpeed, 5f, 90f);

            settings.CreateCustomLabel("Systems");
            settings.CreateFloatSetting("Battery endurance (s)", OnBatteryChanged, ModConfig.BatterySeconds, 60f, 3600f);
            settings.CreateFloatSetting("Link range (m)", OnRangeChanged, ModConfig.LinkRange, 250f, 20000f);
            settings.CreateFloatSetting("Max launch airspeed (m/s)", OnLaunchSpeedChanged, ModConfig.MaxLaunchAirspeed, 5f, 400f);
            settings.CreateBoolSetting("Allow launch at any airspeed", OnAllowHighSpeedChanged, ModConfig.AllowHighSpeedLaunch);
            settings.CreateBoolSetting("Engage autopilot with the goggles", OnAutopilotChanged, ModConfig.AutopilotOnGoggles);
            settings.CreateBoolSetting("Return home on failsafe", OnFailsafeChanged, ModConfig.FailsafeReturnHome);
            settings.CreateBoolSetting("Hold position when the goggles go up", OnHoldChanged, ModConfig.HoldOnGogglesUp);

            settings.CreateCustomLabel("Goggles");
            settings.CreateFloatSetting("Screen distance (m)", OnScreenDistanceChanged, ModConfig.ScreenDistance, 0.08f, 0.35f);
            settings.CreateFloatSetting("Screen width (m)", OnScreenWidthChanged, ModConfig.ScreenWidth, 0.08f, 0.45f);
            settings.CreateFloatSetting("Screen curvature", OnCurvatureChanged, ModConfig.ScreenCurvature, 0f, 1f);
            settings.CreateFloatSetting("Feed resolution (px)", OnResolutionChanged, ModConfig.FeedResolution, 256f, 2048f);
            settings.CreateBoolSetting("Show OSD", OnOsdChanged, ModConfig.ShowOsd);
            settings.CreateBoolSetting("Video breakup on weak signal", OnStaticChanged, ModConfig.EnableStatic);

            settings.CreateCustomLabel("Camera");
            settings.CreateFloatSetting("Field of view (deg)", OnFovChanged, ModConfig.CameraFov, 40f, 140f);
            settings.CreateFloatSetting("Zoomed field of view (deg)", OnMinFovChanged, ModConfig.CameraMinFov, 5f, 60f);
            settings.CreateFloatSetting("Camera uptilt (deg)", OnTiltCameraChanged, ModConfig.CameraTilt, 0f, 60f);
            settings.CreateBoolSetting("Stabilise the camera", OnStabiliseChanged, ModConfig.StabiliseGimbal);

            settings.CreateCustomLabel("Diagnostics");
            settings.CreateBoolSetting("Verbose logging", OnVerboseChanged, ModConfig.VerboseLogging);

            VTOLAPI.CreateSettingsMenu(settings);
            ModLog.Trace("Settings menu registered.");
        }

        // Each setter is its own method so the delegates work on every C# version the
        // game's Unity runtime has shipped with.
        private void OnMode2Changed(bool value) { ModConfig.Mode2Sticks = value; Bootstrap.OnSettingsChanged(); }
        private void OnDeadzoneChanged(float value) { ModConfig.StickDeadzone = value; Bootstrap.OnSettingsChanged(); }
        private void OnExpoChanged(float value) { ModConfig.StickExpo = value; Bootstrap.OnSettingsChanged(); }
        private void OnTiltChanged(float value) { ModConfig.MaxTiltAngle = value; Bootstrap.OnSettingsChanged(); }
        private void OnYawRateChanged(float value) { ModConfig.MaxYawRate = value; Bootstrap.OnSettingsChanged(); }
        private void OnThrustChanged(float value) { ModConfig.ThrustToWeight = value; Bootstrap.OnSettingsChanged(); }
        private void OnClimbChanged(float value) { ModConfig.MaxClimbRate = value; Bootstrap.OnSettingsChanged(); }
        private void OnSpeedChanged(float value) { ModConfig.MaxHorizontalSpeed = value; Bootstrap.OnSettingsChanged(); }
        private void OnBatteryChanged(float value) { ModConfig.BatterySeconds = value; Bootstrap.OnSettingsChanged(); }
        private void OnRangeChanged(float value) { ModConfig.LinkRange = value; Bootstrap.OnSettingsChanged(); }
        private void OnLaunchSpeedChanged(float value) { ModConfig.MaxLaunchAirspeed = value; Bootstrap.OnSettingsChanged(); }
        private void OnAllowHighSpeedChanged(bool value) { ModConfig.AllowHighSpeedLaunch = value; Bootstrap.OnSettingsChanged(); }
        private void OnAutopilotChanged(bool value) { ModConfig.AutopilotOnGoggles = value; Bootstrap.OnSettingsChanged(); }
        private void OnFailsafeChanged(bool value) { ModConfig.FailsafeReturnHome = value; Bootstrap.OnSettingsChanged(); }
        private void OnHoldChanged(bool value) { ModConfig.HoldOnGogglesUp = value; Bootstrap.OnSettingsChanged(); }
        private void OnScreenDistanceChanged(float value) { ModConfig.ScreenDistance = value; Bootstrap.OnSettingsChanged(); }
        private void OnScreenWidthChanged(float value) { ModConfig.ScreenWidth = value; ModConfig.ScreenHeight = value * 9f / 16f; Bootstrap.OnSettingsChanged(); }
        private void OnCurvatureChanged(float value) { ModConfig.ScreenCurvature = value; Bootstrap.OnSettingsChanged(); }
        private void OnResolutionChanged(float value) { ModConfig.FeedResolution = Mathf.RoundToInt(value); Bootstrap.OnSettingsChanged(); }
        private void OnOsdChanged(bool value) { ModConfig.ShowOsd = value; Bootstrap.OnSettingsChanged(); }
        private void OnStaticChanged(bool value) { ModConfig.EnableStatic = value; Bootstrap.OnSettingsChanged(); }
        private void OnFovChanged(float value) { ModConfig.CameraFov = value; Bootstrap.OnSettingsChanged(); }
        private void OnMinFovChanged(float value) { ModConfig.CameraMinFov = value; Bootstrap.OnSettingsChanged(); }
        private void OnTiltCameraChanged(float value) { ModConfig.CameraTilt = value; Bootstrap.OnSettingsChanged(); }
        private void OnStabiliseChanged(bool value) { ModConfig.StabiliseGimbal = value; Bootstrap.OnSettingsChanged(); }
        private void OnVerboseChanged(bool value) { ModConfig.VerboseLogging = value; ModLog.Verbose = value; Bootstrap.OnSettingsChanged(); }
    }
}
