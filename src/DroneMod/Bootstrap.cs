using System;
using DroneMod.Config;
using DroneMod.Core;
using DroneMod.Util;

namespace DroneMod
{
    /// <summary>
    /// Loader-agnostic entry point.
    ///
    /// <see cref="Main"/> is the only file that references VTOL VR's mod loader. If
    /// the loader's API changes, or you would rather run this under BepInEx or any
    /// other injector, delete that file and call <see cref="Initialise"/> from
    /// wherever your loader gives you a hook. Nothing below this point knows or
    /// cares how the mod was started.
    /// </summary>
    public static class Bootstrap
    {
        private static bool _initialised;

        /// <summary>Scenes that are definitely not flyable. Anything else is treated as a flight scene.</summary>
        private static readonly string[] NonFlightScenes =
        {
            "SplashScene",
            "SamplerScene",
            "ReadyRoom",
            "VehicleConfiguration",
            "LoadingScene",
            "MenuScene",
            "CommRadio"
        };

        /// <summary>Starts the mod. Safe to call more than once.</summary>
        public static void Initialise()
        {
            if (_initialised)
            {
                return;
            }

            _initialised = true;

            try
            {
                ModConfig.Load();
                DroneManager.Create();
                ModLog.Info("FPV Drone loaded. " + ModConfig.DeployKey + " deploys, " + ModConfig.GogglesKey + " flips the goggles.");
            }
            catch (Exception e)
            {
                ModLog.Exception(e, "Initialisation failed");
            }
        }

        /// <summary>
        /// Call on every scene change. The scene name is matched loosely so this keeps
        /// working when the game adds or renames scenes.
        /// </summary>
        public static void NotifySceneChanged(string sceneName)
        {
            bool isFlightScene = true;

            if (!string.IsNullOrEmpty(sceneName))
            {
                for (int i = 0; i < NonFlightScenes.Length; i++)
                {
                    if (string.Equals(sceneName, NonFlightScenes[i], StringComparison.OrdinalIgnoreCase))
                    {
                        isFlightScene = false;
                        break;
                    }
                }
            }

            DroneManager manager = DroneManager.Instance;
            if (manager != null)
            {
                manager.OnSceneChanged(isFlightScene);
            }
        }

        /// <summary>Called by the settings menu after any value changes.</summary>
        public static void OnSettingsChanged()
        {
            ModConfig.Save();

            DroneManager manager = DroneManager.Instance;
            if (manager != null)
            {
                manager.ApplySettings();
            }
        }
    }
}
