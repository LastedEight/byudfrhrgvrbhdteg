// ---------------------------------------------------------------------------
// Compile-check stubs for the VTOL VR Mod Loader. NOT part of the mod.
//
// This mirrors the public surface of ModLoader.dll that src/DroneMod/Main.cs
// uses. If the real loader's API has moved on, this file is where the mismatch
// shows up first -- and Main.cs is the only source file affected.
// ---------------------------------------------------------------------------
#pragma warning disable 67

using System;
using UnityEngine;

namespace ModLoader
{
    /// <summary>Scenes the loader reports. Only ToString() is relied on by this mod.</summary>
    public enum VTOLScenes
    {
        SplashScene,
        SamplerScene,
        ReadyRoom,
        VehicleConfiguration,
        LoadingScene,
        Akutan,
        CustomMapBase
    }

    /// <summary>Base class every mod derives from. The loader calls ModLoaded() once.</summary>
    public class VTOLMOD : MonoBehaviour
    {
        public virtual void ModLoaded() { }
    }

    /// <summary>The settings page a mod can register with the loader.</summary>
    public class Settings
    {
        public Settings(VTOLMOD mod) { }
        public void CreateCustomLabel(string label) { }
        public void CreateBoolSetting(string name, Action<bool> setter, bool defaultValue) { }
        public void CreateFloatSetting(string name, Action<float> setter, float defaultValue, float min, float max) { }
        public void CreateIntSetting(string name, Action<int> setter, int defaultValue, int min, int max) { }
    }

    public static class VTOLAPI
    {
        public delegate void SceneChanged(VTOLScenes scene);

        public static event SceneChanged SceneLoaded;

        public static GameObject GetPlayersVehicleGameObject() { return null; }

        public static void CreateSettingsMenu(Settings settings) { }
    }
}
