using UnityEngine;
using DroneMod.Config;
using DroneMod.Util;

namespace DroneMod.Drone
{
    /// <summary>
    /// The drone's camera: a two-axis gimbal, a render texture, zoom, and the
    /// day/low-light/thermal modes.
    ///
    /// The feed is produced by an ordinary Unity camera rendering into a
    /// <see cref="RenderTexture"/>; the goggles then display that texture. Nothing
    /// clever is needed to make it work in VR, because the goggle screen is just
    /// geometry in the world that both eyes happen to be looking at.
    /// </summary>
    internal sealed class DroneCameraRig : MonoBehaviour
    {
        private const float GimbalPitchRate = 45f;
        private const float GimbalYawRate = 60f;
        private const float ZoomRate = 0.8f;

        private Transform _yawJoint;
        private Transform _pitchJoint;
        private Camera _camera;
        private RenderTexture _feed;

        private float _gimbalPitch;
        private float _gimbalYaw;
        private float _zoom;
        private float _displayedFov;

        public CameraMode Mode { get; private set; }

        /// <summary>The live video feed. Null until <see cref="Build"/> has run.</summary>
        public RenderTexture Feed
        {
            get { return _feed; }
        }

        /// <summary>Current gimbal tilt relative to the horizon (stabilised) or the airframe (FPV), degrees.</summary>
        public float GimbalPitch
        {
            get { return _gimbalPitch; }
        }

        /// <summary>Zoom factor, 1 = wide, higher = tighter.</summary>
        public float ZoomFactor
        {
            get { return Mathf.Max(1f, ModConfig.CameraFov / Mathf.Max(1f, _displayedFov)); }
        }

        /// <summary>Colour cast applied to the feed by the current camera mode.</summary>
        public Color ModeTint
        {
            get
            {
                switch (Mode)
                {
                    case CameraMode.LowLight:
                        return new Color(0.55f, 1.35f, 0.65f, 1f);
                    case CameraMode.Thermal:
                        return new Color(1.25f, 0.95f, 0.75f, 1f);
                    default:
                        return Color.white;
                }
            }
        }

        public string ModeLabel
        {
            get
            {
                switch (Mode)
                {
                    case CameraMode.LowLight:
                        return "NV";
                    case CameraMode.Thermal:
                        return "IR";
                    default:
                        return "DAY";
                }
            }
        }

        /// <summary>Creates the gimbal joints and the camera. Call once, from the builder.</summary>
        public void Build(Transform mount)
        {
            _yawJoint = new GameObject("GimbalYaw").transform;
            _yawJoint.SetParent(mount, false);

            _pitchJoint = new GameObject("GimbalPitch").transform;
            _pitchJoint.SetParent(_yawJoint, false);

            GameObject cameraObject = new GameObject("DroneCamera");
            cameraObject.transform.SetParent(_pitchJoint, false);

            _camera = cameraObject.AddComponent<Camera>();
            _camera.fieldOfView = ModConfig.CameraFov;
            _camera.nearClipPlane = 0.05f;
            _camera.farClipPlane = 40000f;
            _camera.cullingMask = LayerUtil.DroneCameraMask();
            _camera.clearFlags = CameraClearFlags.Skybox;
            _camera.backgroundColor = Color.black;
            _camera.stereoTargetEye = StereoTargetEyeMask.None;

            // Negative depth keeps this well behind the pilot's own camera in the render order.
            _camera.depth = -50f;

            AllocateFeed();

            _gimbalPitch = -ModConfig.CameraTilt;
            _displayedFov = ModConfig.CameraFov;
            Mode = CameraMode.Daylight;

            SetActive(false);
        }

        private void AllocateFeed()
        {
            ReleaseFeed();

            int size = Mathf.Clamp(ModConfig.FeedResolution, 256, 2048);

            // 16:9 is what FPV goggles are, and it costs less than a square texture.
            int width = size;
            int height = Mathf.RoundToInt(size * 9f / 16f);

            _feed = new RenderTexture(width, height, 24, RenderTextureFormat.Default);
            _feed.name = "DroneFeed";
            _feed.antiAliasing = 2;
            _feed.filterMode = FilterMode.Bilinear;
            _feed.wrapMode = TextureWrapMode.Clamp;
            _feed.Create();

            if (_camera != null)
            {
                _camera.targetTexture = _feed;
                _camera.aspect = (float)width / height;
            }

            ModLog.Trace("Allocated video feed at " + width + "x" + height + ".");
        }

        private void ReleaseFeed()
        {
            if (_feed == null)
            {
                return;
            }

            if (_camera != null)
            {
                _camera.targetTexture = null;
            }

            _feed.Release();
            Object.Destroy(_feed);
            _feed = null;
        }

        /// <summary>
        /// Rendering the feed costs a full extra camera pass, so it only runs while
        /// the pilot actually has the goggles over their eyes.
        /// </summary>
        public void SetActive(bool active)
        {
            if (_camera != null)
            {
                _camera.enabled = active;
            }
        }

        public void CycleMode()
        {
            switch (Mode)
            {
                case CameraMode.Daylight:
                    Mode = CameraMode.LowLight;
                    break;
                case CameraMode.LowLight:
                    Mode = CameraMode.Thermal;
                    break;
                default:
                    Mode = CameraMode.Daylight;
                    break;
            }

            // Low-light and thermal sensors are physically wider and do not zoom the same way.
            ModLog.Trace("Camera mode: " + ModeLabel);
        }

        public void Recentre()
        {
            _gimbalYaw = 0f;
            _gimbalPitch = -ModConfig.CameraTilt;
            _zoom = 0f;
        }

        /// <summary>
        /// Drives the gimbal. When stabilised, the pitch joint is held against the
        /// world horizon so airframe attitude does not shake the picture; in FPV mode
        /// the camera is bolted to the airframe with the usual upward tilt.
        /// </summary>
        public void UpdateGimbal(float pitchDemand, float yawDemand, float zoomDemand, float deltaTime)
        {
            _gimbalPitch = Mathf.Clamp(_gimbalPitch + pitchDemand * GimbalPitchRate * deltaTime, -100f, 45f);
            _gimbalYaw = Mathf.Clamp(_gimbalYaw + yawDemand * GimbalYawRate * deltaTime, -120f, 120f);
            _zoom = Mathf.Clamp01(_zoom + zoomDemand * ZoomRate * deltaTime);

            if (_yawJoint == null || _pitchJoint == null)
            {
                return;
            }

            if (ModConfig.StabiliseGimbal)
            {
                // Decouple from airframe roll and pitch: build the rotation in world
                // space from the airframe's heading plus the commanded gimbal angles.
                float heading = MathUtil.HeadingOf(transform.forward);
                Quaternion target = Quaternion.Euler(-_gimbalPitch, heading + _gimbalYaw, 0f);
                _pitchJoint.rotation = Quaternion.Slerp(_pitchJoint.rotation, target, 1f - Mathf.Exp(-deltaTime * 14f));
                _yawJoint.localRotation = Quaternion.identity;
            }
            else
            {
                _yawJoint.localRotation = Quaternion.Euler(0f, _gimbalYaw, 0f);
                _pitchJoint.localRotation = Quaternion.Euler(-_gimbalPitch, 0f, 0f);
            }

            if (_camera != null)
            {
                float targetFov = Mathf.Lerp(ModConfig.CameraFov, ModConfig.CameraMinFov, _zoom);
                _displayedFov = MathUtil.Smooth(_displayedFov, targetFov, 8f, deltaTime);
                _camera.fieldOfView = _displayedFov;
            }
        }

        /// <summary>Re-allocates the render texture after a resolution change in the settings menu.</summary>
        public void ReapplyResolution()
        {
            if (_feed == null)
            {
                return;
            }

            int size = Mathf.Clamp(ModConfig.FeedResolution, 256, 2048);
            if (_feed.width == size)
            {
                return;
            }

            bool wasActive = _camera != null && _camera.enabled;
            AllocateFeed();
            SetActive(wasActive);
        }

        private void OnDestroy()
        {
            ReleaseFeed();
        }
    }
}
