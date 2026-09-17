using UnityEngine;
using DroneMod.Util;

namespace DroneMod.Drone
{
    /// <summary>
    /// Assembles the airframe out of Unity primitives at runtime.
    ///
    /// A hand-modelled quad in an AssetBundle would look better, but a bundle has to
    /// be built against the exact Unity version the game ships and breaks whenever
    /// that changes. Primitives cost nothing to maintain and read perfectly well
    /// through a 1024-pixel FPV feed.
    /// </summary>
    internal static class DroneBuilder
    {
        /// <summary>Distance from the centre to each motor, metres. A 5-inch quad is about this size.</summary>
        public const float ArmLength = 0.115f;

        public const float Mass = 0.78f;

        public static DroneController Build()
        {
            GameObject root = new GameObject("FPVDrone");

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = Mass;
            body.drag = 0f;           // Drag is modelled per-axis by the flight model.
            body.angularDrag = 0f;    // Likewise angular damping.
            body.useGravity = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.maxAngularVelocity = 40f;

            BuildColliders(root);

            Material frameMaterial = AssetFactory.CreateLitMaterial(new Color(0.09f, 0.09f, 0.11f), 0.35f, 0.15f);
            Material accentMaterial = AssetFactory.CreateLitMaterial(new Color(0.75f, 0.18f, 0.06f), 0.5f, 0.2f);
            Material propMaterial = AssetFactory.CreateLitMaterial(new Color(0.15f, 0.15f, 0.18f), 0.2f, 0.0f);
            Material lensMaterial = AssetFactory.CreateLitMaterial(new Color(0.02f, 0.05f, 0.12f), 0.95f, 0.4f);

            BuildFuselage(root.transform, frameMaterial, accentMaterial);
            PropSpinner[] props = BuildArmsAndProps(root.transform, frameMaterial, propMaterial);
            Transform cameraMount = BuildCameraPod(root.transform, frameMaterial, lensMaterial);
            BuildNavigationLights(root.transform);

            DroneCameraRig cameraRig = root.AddComponent<DroneCameraRig>();
            cameraRig.Build(cameraMount);

            DroneBattery battery = root.AddComponent<DroneBattery>();

            DroneLink link = root.AddComponent<DroneLink>();
            link.Initialise(root.transform);

            DroneAudio audio = root.AddComponent<DroneAudio>();
            audio.Build();

            DroneAutopilot autopilot = root.AddComponent<DroneAutopilot>();

            DroneController controller = root.AddComponent<DroneController>();
            controller.Initialise(body, cameraRig, battery, link, audio, autopilot, props);

            ModLog.Trace("Airframe assembled.");
            return controller;
        }

        private static void BuildColliders(GameObject root)
        {
            // One box for the centre stack...
            BoxCollider core = root.AddComponent<BoxCollider>();
            core.center = Vector3.zero;
            core.size = new Vector3(0.10f, 0.05f, 0.14f);

            // ...and four spheres at the motors, which is what actually hits things.
            for (int i = 0; i < 4; i++)
            {
                Vector3 position = MotorPosition(i);
                SphereCollider motor = root.AddComponent<SphereCollider>();
                motor.center = position;
                motor.radius = 0.055f;
            }
        }

        /// <summary>Motor positions in the classic X layout, front-right first, going clockwise.</summary>
        public static Vector3 MotorPosition(int index)
        {
            float x = (index == 0 || index == 3) ? ArmLength : -ArmLength;
            float z = (index == 0 || index == 1) ? ArmLength : -ArmLength;
            return new Vector3(x, 0f, z);
        }

        private static void BuildFuselage(Transform root, Material frame, Material accent)
        {
            AssetFactory.CreateVisualPrimitive(
                PrimitiveType.Cube, root, "Body",
                new Vector3(0f, 0f, 0f), Quaternion.identity,
                new Vector3(0.075f, 0.040f, 0.135f), frame);

            // Battery strapped on top, as everyone does.
            AssetFactory.CreateVisualPrimitive(
                PrimitiveType.Cube, root, "Battery",
                new Vector3(0f, 0.032f, -0.012f), Quaternion.identity,
                new Vector3(0.055f, 0.030f, 0.090f), accent);

            // Video transmitter antenna, pointing up and back.
            AssetFactory.CreateVisualPrimitive(
                PrimitiveType.Cylinder, root, "Antenna",
                new Vector3(0f, 0.055f, -0.080f), Quaternion.Euler(-25f, 0f, 0f),
                new Vector3(0.006f, 0.045f, 0.006f), frame);

            AssetFactory.CreateVisualPrimitive(
                PrimitiveType.Sphere, root, "AntennaCap",
                new Vector3(0f, 0.095f, -0.098f), Quaternion.identity,
                new Vector3(0.018f, 0.018f, 0.018f), accent);

            // Landing legs.
            for (int i = 0; i < 4; i++)
            {
                Vector3 motor = MotorPosition(i);
                AssetFactory.CreateVisualPrimitive(
                    PrimitiveType.Cylinder, root, "Leg" + i,
                    new Vector3(motor.x * 0.6f, -0.030f, motor.z * 0.6f), Quaternion.identity,
                    new Vector3(0.005f, 0.030f, 0.005f), frame);
            }
        }

        private static PropSpinner[] BuildArmsAndProps(Transform root, Material frame, Material prop)
        {
            PropSpinner[] spinners = new PropSpinner[4];

            for (int i = 0; i < 4; i++)
            {
                Vector3 motor = MotorPosition(i);

                // Arm: a thin box from the centre out to the motor.
                GameObject arm = AssetFactory.CreateVisualPrimitive(
                    PrimitiveType.Cube, root, "Arm" + i,
                    motor * 0.5f, Quaternion.identity,
                    new Vector3(0.016f, 0.010f, 0.016f), frame);
                arm.transform.localRotation = Quaternion.LookRotation(new Vector3(motor.x, 0f, motor.z).normalized, Vector3.up);
                arm.transform.localScale = new Vector3(0.016f, 0.010f, motor.magnitude * 2f * 0.5f + 0.02f);

                AssetFactory.CreateVisualPrimitive(
                    PrimitiveType.Cylinder, root, "Motor" + i,
                    motor + new Vector3(0f, 0.008f, 0f), Quaternion.identity,
                    new Vector3(0.028f, 0.012f, 0.028f), frame);

                GameObject hub = new GameObject("PropHub" + i);
                hub.transform.SetParent(root, false);
                hub.transform.localPosition = motor + new Vector3(0f, 0.024f, 0f);

                // Two blades per prop, offset 180 degrees.
                for (int blade = 0; blade < 2; blade++)
                {
                    AssetFactory.CreateVisualPrimitive(
                        PrimitiveType.Cube, hub.transform, "Blade" + blade,
                        new Vector3(blade == 0 ? 0.032f : -0.032f, 0f, 0f),
                        Quaternion.Euler(0f, 0f, blade == 0 ? 8f : -8f),
                        new Vector3(0.064f, 0.002f, 0.014f), prop);
                }

                PropSpinner spinner = hub.AddComponent<PropSpinner>();
                // Diagonal pairs turn the same way so the yaw torques cancel.
                spinner.Direction = (i == 0 || i == 2) ? 1f : -1f;
                spinners[i] = spinner;
            }

            return spinners;
        }

        private static Transform BuildCameraPod(Transform root, Material frame, Material lens)
        {
            GameObject pod = AssetFactory.CreateVisualPrimitive(
                PrimitiveType.Cube, root, "CameraPod",
                new Vector3(0f, 0.008f, 0.072f), Quaternion.identity,
                new Vector3(0.036f, 0.030f, 0.030f), frame);

            AssetFactory.CreateVisualPrimitive(
                PrimitiveType.Sphere, pod.transform, "Lens",
                new Vector3(0f, 0.05f, 0.45f), Quaternion.identity,
                new Vector3(0.55f, 0.65f, 0.55f), lens);

            // The gimbal hangs off an empty at the nose so the pod mesh itself can
            // stay bolted to the airframe while the camera moves.
            GameObject mount = new GameObject("GimbalMount");
            mount.transform.SetParent(root, false);
            mount.transform.localPosition = new Vector3(0f, 0.010f, 0.090f);
            return mount.transform;
        }

        private static void BuildNavigationLights(Transform root)
        {
            // Green on the right, red on the left, as on any aircraft. These are the
            // only way to tell the drone's orientation once it is more than 50 m away.
            CreateNavLight(root, "NavRight", new Vector3(ArmLength, 0.012f, ArmLength * 0.2f), new Color(0.1f, 1f, 0.2f));
            CreateNavLight(root, "NavLeft", new Vector3(-ArmLength, 0.012f, ArmLength * 0.2f), new Color(1f, 0.15f, 0.1f));
            CreateNavLight(root, "NavTail", new Vector3(0f, 0.012f, -ArmLength), new Color(1f, 1f, 1f));
        }

        private static void CreateNavLight(Transform root, string name, Vector3 position, Color color)
        {
            Material emissive = AssetFactory.CreateUnlitMaterial(color);
            GameObject bulb = AssetFactory.CreateVisualPrimitive(
                PrimitiveType.Sphere, root, name, position, Quaternion.identity,
                new Vector3(0.012f, 0.012f, 0.012f), emissive);

            Light light = bulb.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = 2.5f;
            light.intensity = 1.6f;
        }
    }
}
