using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using DroneMod.Util;

namespace DroneMod.Core
{
    /// <summary>
    /// Every point where this mod touches VTOL VR's own code goes through here, by
    /// reflection and by name.
    ///
    /// The reason is version drift: VTOL VR ships updates that rename and reshape
    /// Assembly-CSharp regularly, and a mod that binds those types at compile time
    /// stops loading entirely the day one of them moves. Looking members up by name
    /// means a renamed member degrades one feature (a warning in the log, a keyboard
    /// fallback) instead of killing the mod. It also means the DLL builds against
    /// nothing but UnityEngine plus the loader.
    ///
    /// Everything here is best-effort and every accessor is allowed to fail.
    /// </summary>
    internal static class GameBridge
    {
        private static readonly Dictionary<string, Type> TypeCache = new Dictionary<string, Type>();
        private static readonly HashSet<string> WarnedOnce = new HashSet<string>();

        private static Transform _head;
        private static Camera _headCamera;
        private static GameObject _playerVehicle;
        private static Rigidbody _playerRigidbody;
        private static Component _leftController;
        private static Component _rightController;
        private static float _lastControllerScan = -999f;

        /// <summary>Called on every scene change; all cached scene objects become invalid.</summary>
        public static void Invalidate()
        {
            _head = null;
            _headCamera = null;
            _playerVehicle = null;
            _playerRigidbody = null;
            _leftController = null;
            _rightController = null;
            _lastControllerScan = -999f;
            ModLog.Trace("GameBridge cache invalidated.");
        }

        // ------------------------------------------------------------ type lookup

        /// <summary>Finds a loaded type by its simple name, trying each candidate in order.</summary>
        public static Type FindType(params string[] simpleNames)
        {
            for (int i = 0; i < simpleNames.Length; i++)
            {
                string name = simpleNames[i];
                Type cached;
                if (TypeCache.TryGetValue(name, out cached))
                {
                    if (cached != null)
                    {
                        return cached;
                    }

                    continue;
                }

                Type found = ScanForType(name);
                TypeCache[name] = found;
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Type ScanForType(string simpleName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int a = 0; a < assemblies.Length; a++)
            {
                Type[] types;
                try
                {
                    types = assemblies[a].GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }
                catch (Exception)
                {
                    continue;
                }

                if (types == null)
                {
                    continue;
                }

                for (int t = 0; t < types.Length; t++)
                {
                    if (types[t] != null && types[t].Name == simpleName)
                    {
                        return types[t];
                    }
                }
            }

            return null;
        }

        // --------------------------------------------------------- member access

        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
        private const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

        public static bool TryGetMember(object instance, out object value, params string[] names)
        {
            value = null;
            if (instance == null)
            {
                return false;
            }

            Type type = instance.GetType();
            for (int i = 0; i < names.Length; i++)
            {
                FieldInfo field = type.GetField(names[i], AnyInstance);
                if (field != null)
                {
                    value = field.GetValue(instance);
                    return true;
                }

                PropertyInfo property = type.GetProperty(names[i], AnyInstance);
                if (property != null && property.CanRead)
                {
                    value = property.GetValue(instance, null);
                    return true;
                }
            }

            return false;
        }

        public static bool TryGetStaticMember(Type type, out object value, params string[] names)
        {
            value = null;
            if (type == null)
            {
                return false;
            }

            for (int i = 0; i < names.Length; i++)
            {
                FieldInfo field = type.GetField(names[i], AnyStatic);
                if (field != null)
                {
                    value = field.GetValue(null);
                    return true;
                }

                PropertyInfo property = type.GetProperty(names[i], AnyStatic);
                if (property != null && property.CanRead)
                {
                    value = property.GetValue(null, null);
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetFloat(object instance, out float value, params string[] names)
        {
            value = 0f;
            object raw;
            if (!TryGetMember(instance, out raw, names) || raw == null)
            {
                return false;
            }

            if (raw is float)
            {
                value = (float)raw;
                return true;
            }

            if (raw is double)
            {
                value = (float)(double)raw;
                return true;
            }

            return false;
        }

        private static bool TryGetVector2(object instance, out Vector2 value, params string[] names)
        {
            value = Vector2.zero;
            object raw;
            if (!TryGetMember(instance, out raw, names) || raw == null)
            {
                return false;
            }

            if (raw is Vector2)
            {
                value = (Vector2)raw;
                return true;
            }

            if (raw is Vector3)
            {
                Vector3 v3 = (Vector3)raw;
                value = new Vector2(v3.x, v3.y);
                return true;
            }

            return false;
        }

        private static void WarnOnce(string key, string message)
        {
            if (WarnedOnce.Add(key))
            {
                ModLog.Warn(message);
            }
        }

        // ------------------------------------------------------------------ head

        /// <summary>
        /// The HMD transform. The goggles are parented to this, which is what makes
        /// them behave like goggles: they stay locked to the face when the pilot
        /// looks around, rather than floating in the cockpit.
        /// </summary>
        public static Transform Head
        {
            get
            {
                if (_head != null)
                {
                    return _head;
                }

                Type vrHead = FindType("VRHead");
                if (vrHead != null)
                {
                    object instance;
                    if (TryGetStaticMember(vrHead, out instance, "instance", "_instance", "Instance") && instance != null)
                    {
                        Component component = instance as Component;
                        if (component != null)
                        {
                            _head = component.transform;
                            ModLog.Trace("Head transform resolved from VRHead.instance.");
                            return _head;
                        }
                    }
                }

                Camera camera = HeadCamera;
                if (camera != null)
                {
                    _head = camera.transform;
                    WarnOnce("head-fallback", "VRHead was not found; falling back to the main camera for head tracking.");
                    return _head;
                }

                return null;
            }
        }

        /// <summary>The camera the pilot actually looks through. Used only for culling-mask sanity checks.</summary>
        public static Camera HeadCamera
        {
            get
            {
                if (_headCamera != null)
                {
                    return _headCamera;
                }

                _headCamera = Camera.main;
                if (_headCamera == null)
                {
                    Camera[] cameras = Camera.allCameras;
                    float best = float.NegativeInfinity;
                    for (int i = 0; i < cameras.Length; i++)
                    {
                        if (cameras[i] != null && cameras[i].enabled && cameras[i].depth > best)
                        {
                            best = cameras[i].depth;
                            _headCamera = cameras[i];
                        }
                    }
                }

                return _headCamera;
            }
        }

        // --------------------------------------------------------------- vehicle

        /// <summary>The player's aircraft, used as the launch platform and the RTH home reference.</summary>
        public static GameObject PlayerVehicle
        {
            get
            {
                if (_playerVehicle != null)
                {
                    return _playerVehicle;
                }

                // Preferred: the mod loader's own helper, which already knows how to find this.
                Type api = FindType("VTOLAPI");
                if (api != null)
                {
                    try
                    {
                        MethodInfo method = api.GetMethod("GetPlayersVehicleGameObject", AnyStatic);
                        if (method != null)
                        {
                            _playerVehicle = method.Invoke(null, null) as GameObject;
                            if (_playerVehicle != null)
                            {
                                ModLog.Trace("Player vehicle resolved through VTOLAPI.");
                                return _playerVehicle;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        ModLog.Trace("VTOLAPI.GetPlayersVehicleGameObject failed: " + e.Message);
                    }
                }

                // Fallback: whatever object owns the flight-info component.
                Component flightInfo = FindComponent("FlightInfo", "PlayerVehicleSetup", "VehicleMaster");
                if (flightInfo != null)
                {
                    _playerVehicle = flightInfo.transform.root.gameObject;
                    return _playerVehicle;
                }

                // Last resort: the rigidbody the head is parented under.
                Transform head = Head;
                if (head != null)
                {
                    Rigidbody body = head.GetComponentInParent<Rigidbody>();
                    if (body != null)
                    {
                        _playerVehicle = body.gameObject;
                        WarnOnce("vehicle-fallback", "Player vehicle resolved from the head's parent rigidbody.");
                        return _playerVehicle;
                    }
                }

                return null;
            }
        }

        public static Rigidbody PlayerRigidbody
        {
            get
            {
                if (_playerRigidbody != null)
                {
                    return _playerRigidbody;
                }

                GameObject vehicle = PlayerVehicle;
                if (vehicle != null)
                {
                    // Not ??: Unity overloads == for destroyed objects, and the
                    // null-coalescing operator does not honour that overload.
                    Rigidbody direct = vehicle.GetComponent<Rigidbody>();
                    _playerRigidbody = direct != null ? direct : vehicle.GetComponentInChildren<Rigidbody>();
                }

                return _playerRigidbody;
            }
        }

        public static Vector3 PlayerVelocity
        {
            get
            {
                Rigidbody body = PlayerRigidbody;
                return body != null ? body.velocity : Vector3.zero;
            }
        }

        private static Component FindComponent(params string[] typeNames)
        {
            Type type = FindType(typeNames);
            if (type == null)
            {
                return null;
            }

            try
            {
                return UnityEngine.Object.FindObjectOfType(type) as Component;
            }
            catch (Exception e)
            {
                ModLog.Trace("FindObjectOfType(" + type.Name + ") failed: " + e.Message);
                return null;
            }
        }

        // ------------------------------------------------------------ controllers

        /// <summary>
        /// Locates the two VR hand controllers. Rescanned periodically because the
        /// controller objects are created and destroyed with the flight scene.
        /// </summary>
        private static void ScanControllers()
        {
            if (Time.unscaledTime - _lastControllerScan < 2f && (_leftController != null || _rightController != null))
            {
                return;
            }

            _lastControllerScan = Time.unscaledTime;

            Type controllerType = FindType("VRHandController", "VRHand", "VRController");
            if (controllerType == null)
            {
                WarnOnce("controller-type", "VR hand controller type not found; the drone will use keyboard input only.");
                return;
            }

            UnityEngine.Object[] found;
            try
            {
                found = UnityEngine.Object.FindObjectsOfType(controllerType);
            }
            catch (Exception e)
            {
                ModLog.Trace("FindObjectsOfType(controller) failed: " + e.Message);
                return;
            }

            for (int i = 0; i < found.Length; i++)
            {
                Component component = found[i] as Component;
                if (component == null)
                {
                    continue;
                }

                bool? isRight = IsRightHand(component);
                if (isRight == true)
                {
                    _rightController = component;
                }
                else if (isRight == false)
                {
                    _leftController = component;
                }
            }
        }

        private static bool? IsRightHand(Component controller)
        {
            object handValue;
            if (TryGetMember(controller, out handValue, "controllerType", "hand", "handType", "side") && handValue != null)
            {
                string text = handValue.ToString().ToLowerInvariant();
                if (text.Contains("right"))
                {
                    return true;
                }

                if (text.Contains("left"))
                {
                    return false;
                }
            }

            string name = controller.gameObject.name.ToLowerInvariant();
            if (name.Contains("right"))
            {
                return true;
            }

            if (name.Contains("left"))
            {
                return false;
            }

            return null;
        }

        /// <summary>Thumbstick deflection for one hand, in [-1, 1] per axis. False when no controller is available.</summary>
        public static bool TryGetThumbstick(bool rightHand, out Vector2 axis)
        {
            axis = Vector2.zero;
            ScanControllers();

            Component controller = rightHand ? _rightController : _leftController;
            if (controller == null)
            {
                return false;
            }

            return TryGetVector2(controller, out axis, "thumbstickAxis", "stickAxis", "thumbstick", "joystickAxis", "primary2DAxis");
        }

        /// <summary>Trigger travel for one hand, in [0, 1].</summary>
        public static bool TryGetTrigger(bool rightHand, out float value)
        {
            value = 0f;
            ScanControllers();

            Component controller = rightHand ? _rightController : _leftController;
            if (controller == null)
            {
                return false;
            }

            return TryGetFloat(controller, out value, "triggerAxis", "triggerValue", "trigger", "indexTrigger");
        }

        /// <summary>Grip travel for one hand, in [0, 1]. Used as the gimbal-control modifier.</summary>
        public static bool TryGetGrip(bool rightHand, out float value)
        {
            value = 0f;
            ScanControllers();

            Component controller = rightHand ? _rightController : _leftController;
            if (controller == null)
            {
                return false;
            }

            if (TryGetFloat(controller, out value, "gripAxis", "gripValue", "grip", "handGrip"))
            {
                return true;
            }

            object raw;
            if (TryGetMember(controller, out raw, "gripPressed", "gripping", "isGripping") && raw is bool)
            {
                value = (bool)raw ? 1f : 0f;
                return true;
            }

            return false;
        }

        // -------------------------------------------------------------- autopilot

        /// <summary>
        /// Best-effort autopilot engage, so the aircraft is not left hand-flown while
        /// the pilot is inside the goggles. Returns false when no autopilot could be
        /// found, in which case the caller warns the pilot rather than pretending.
        /// </summary>
        public static bool TrySetAutopilot(bool enabled)
        {
            Component autopilot = FindComponent("Autopilot", "AutoPilot", "AutopilotModule");
            if (autopilot == null)
            {
                WarnOnce("autopilot-missing", "No autopilot component found; the aircraft will not be stabilised automatically.");
                return false;
            }

            Type type = autopilot.GetType();
            string[] methodNames = { "SetAutopilot", "SetAutoPilot", "SetEnabled", "ToggleAutopilot", "EnableAutopilot" };

            for (int i = 0; i < methodNames.Length; i++)
            {
                MethodInfo method = type.GetMethod(methodNames[i], AnyInstance, null, new[] { typeof(bool) }, null);
                if (method != null)
                {
                    try
                    {
                        method.Invoke(autopilot, new object[] { enabled });
                        ModLog.Trace("Autopilot " + (enabled ? "engaged" : "disengaged") + " via " + methodNames[i] + ".");
                        return true;
                    }
                    catch (Exception e)
                    {
                        ModLog.Trace(methodNames[i] + " threw: " + e.Message);
                    }
                }
            }

            string[] fieldNames = { "autopilotEnabled", "apEnabled", "enabled", "isEnabled", "autoPilotOn" };
            for (int i = 0; i < fieldNames.Length; i++)
            {
                FieldInfo field = type.GetField(fieldNames[i], AnyInstance);
                if (field != null && field.FieldType == typeof(bool))
                {
                    field.SetValue(autopilot, enabled);
                    ModLog.Trace("Autopilot flag '" + fieldNames[i] + "' set to " + enabled + ".");
                    return true;
                }

                PropertyInfo property = type.GetProperty(fieldNames[i], AnyInstance);
                if (property != null && property.CanWrite && property.PropertyType == typeof(bool))
                {
                    property.SetValue(autopilot, enabled, null);
                    ModLog.Trace("Autopilot property '" + fieldNames[i] + "' set to " + enabled + ".");
                    return true;
                }
            }

            WarnOnce("autopilot-shape", "An autopilot component was found but exposes no recognised on/off member.");
            return false;
        }

        // ------------------------------------------------------------------- misc

        /// <summary>
        /// The game's wind vector, if the weather system exposes one. Falls back to
        /// still air, which only costs a little atmosphere.
        /// </summary>
        public static Vector3 GetWind()
        {
            Type weather = FindType("WeatherSystem", "WindManager", "EnvironmentManager");
            if (weather == null)
            {
                return Vector3.zero;
            }

            object instance;
            if (!TryGetStaticMember(weather, out instance, "instance", "_instance", "Instance") || instance == null)
            {
                return Vector3.zero;
            }

            object raw;
            if (TryGetMember(instance, out raw, "windVector", "wind", "currentWind", "windVelocity") && raw is Vector3)
            {
                return (Vector3)raw;
            }

            return Vector3.zero;
        }

        /// <summary>True when a flyable scene is up, as opposed to the ready room or a menu.</summary>
        public static bool InFlightScene
        {
            get
            {
                return FindComponent("FlightSceneManager") != null || PlayerVehicle != null;
            }
        }
    }
}
