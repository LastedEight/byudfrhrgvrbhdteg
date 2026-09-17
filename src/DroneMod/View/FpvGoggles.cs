using UnityEngine;
using DroneMod.Config;
using DroneMod.Core;
using DroneMod.Drone;
using DroneMod.Util;

namespace DroneMod.View
{
    /// <summary>
    /// The FPV goggles: a physical visor parented to the pilot's head that flips down
    /// over the eyes and shows the drone's camera feed.
    ///
    /// Parenting to the HMD rather than to the cockpit is the whole trick. Once the
    /// screen is locked to the head it behaves exactly like real goggles -- looking
    /// around does not move the picture, and flipping the visor up gives you the
    /// cockpit back instantly, which you need when your jet is still flying itself
    /// somewhere.
    /// </summary>
    internal sealed class FpvGoggles : MonoBehaviour
    {
        private const float StowedAngle = -102f;
        private const float FlipSpeed = 7f;
        private const float StaticRefreshInterval = 0.05f;

        private Transform _hinge;
        private Transform _screen;
        private MeshRenderer _screenRenderer;
        private Material _screenMaterial;
        private MeshRenderer _staticRenderer;
        private Material _staticMaterial;
        private Texture2D _staticTexture;
        private TextMesh _noSignal;
        private readonly OsdRenderer _osd = new OsdRenderer();

        private Transform _head;
        private float _currentAngle = StowedAngle;
        private float _lastStaticRefresh;
        private bool _down;
        private float _cameraRefresh;

        /// <summary>True when the visor is over the pilot's eyes.</summary>
        public bool IsDown
        {
            get { return _down; }
        }

        /// <summary>True once the visor has finished travelling down and the feed is fully in view.</summary>
        public bool IsFullyDown
        {
            get { return _down && _currentAngle > -6f; }
        }

        public OsdRenderer Osd
        {
            get { return _osd; }
        }

        /// <summary>
        /// The screen geometry this instance was built with. Changing any of it in the
        /// settings menu needs a rebuild, because the mesh is generated once.
        /// </summary>
        public ScreenGeometry BuiltGeometry { get; private set; }

        // ------------------------------------------------------------------- build

        public void Build(Transform head)
        {
            _head = head;

            transform.SetParent(head, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            // The hinge sits above and slightly in front of the eyes, which is where a
            // real headband pivot is; rotating about it swings the visor onto the forehead.
            GameObject hinge = new GameObject("Hinge");
            hinge.transform.SetParent(transform, false);
            hinge.transform.localPosition = new Vector3(0f, 0.055f, 0.045f);
            _hinge = hinge.transform;

            BuildShroud();
            BuildScreen();

            _osd.Build(_screen, ModConfig.ScreenWidth, ModConfig.ScreenHeight);
            BuiltGeometry = ScreenGeometry.FromConfig();

            LayerUtil.SetLayerRecursively(transform, LayerUtil.GoggleLayer);
            ApplyAngle(StowedAngle);

            ModLog.Trace("Goggles built and parented to the head.");
        }

        private void BuildShroud()
        {
            // An opaque hood around the screen. Without it you would see the cockpit
            // through the gaps and the illusion falls apart immediately.
            Material black = AssetFactory.CreateUnlitMaterial(Color.black);

            float w = ModConfig.ScreenWidth;
            float h = ModConfig.ScreenHeight;
            float d = ModConfig.ScreenDistance - 0.045f;   // relative to the hinge

            // Backplate, just beyond the screen, blocking everything ahead.
            AssetFactory.CreateVisualPrimitive(PrimitiveType.Cube, _hinge, "Backplate",
                new Vector3(0f, -0.055f, d + 0.012f), Quaternion.identity,
                new Vector3(w * 2.4f, h * 3.4f, 0.004f), black);

            // Hood: four panels running back from the backplate toward the face.
            float hoodDepth = 0.075f;
            AssetFactory.CreateVisualPrimitive(PrimitiveType.Cube, _hinge, "HoodTop",
                new Vector3(0f, -0.055f + h * 1.7f, d - hoodDepth * 0.5f), Quaternion.identity,
                new Vector3(w * 2.4f, 0.004f, hoodDepth), black);
            AssetFactory.CreateVisualPrimitive(PrimitiveType.Cube, _hinge, "HoodBottom",
                new Vector3(0f, -0.055f - h * 1.7f, d - hoodDepth * 0.5f), Quaternion.identity,
                new Vector3(w * 2.4f, 0.004f, hoodDepth), black);
            AssetFactory.CreateVisualPrimitive(PrimitiveType.Cube, _hinge, "HoodLeft",
                new Vector3(-w * 1.2f, -0.055f, d - hoodDepth * 0.5f), Quaternion.identity,
                new Vector3(0.004f, h * 3.4f, hoodDepth), black);
            AssetFactory.CreateVisualPrimitive(PrimitiveType.Cube, _hinge, "HoodRight",
                new Vector3(w * 1.2f, -0.055f, d - hoodDepth * 0.5f), Quaternion.identity,
                new Vector3(0.004f, h * 3.4f, hoodDepth), black);

            // Cosmetic shell, so the goggles read as an object when flipped up.
            AssetFactory.CreateVisualPrimitive(PrimitiveType.Cube, _hinge, "Shell",
                new Vector3(0f, -0.055f, d + 0.018f), Quaternion.identity,
                new Vector3(w * 2.5f, h * 3.5f, 0.010f),
                AssetFactory.CreateLitMaterial(new Color(0.12f, 0.12f, 0.14f), 0.3f, 0.1f));
        }

        private void BuildScreen()
        {
            GameObject screen = new GameObject("Screen");
            screen.transform.SetParent(_hinge, false);
            screen.transform.localPosition = new Vector3(0f, -0.055f, ModConfig.ScreenDistance - 0.045f);
            screen.transform.localRotation = Quaternion.identity;
            _screen = screen.transform;

            MeshFilter filter = screen.AddComponent<MeshFilter>();
            filter.mesh = AssetFactory.CreateCurvedScreen(
                ModConfig.ScreenWidth, ModConfig.ScreenHeight, ModConfig.ScreenCurvature, 16);

            _screenRenderer = screen.AddComponent<MeshRenderer>();
            _screenMaterial = AssetFactory.CreateScreenMaterial(Color.white, 3000);
            _screenRenderer.sharedMaterial = _screenMaterial;

            // A second, slightly nearer surface carries the video breakup so it can be
            // cross-faded in without touching the feed itself.
            GameObject noise = new GameObject("Static");
            noise.transform.SetParent(_screen, false);
            noise.transform.localPosition = new Vector3(0f, 0f, -0.0008f);

            MeshFilter noiseFilter = noise.AddComponent<MeshFilter>();
            noiseFilter.mesh = AssetFactory.CreateCurvedScreen(
                ModConfig.ScreenWidth, ModConfig.ScreenHeight, ModConfig.ScreenCurvature, 16);

            _staticTexture = AssetFactory.CreateStaticTexture(96);
            _staticMaterial = AssetFactory.CreateScreenMaterial(new Color(1f, 1f, 1f, 0f), 3100);
            _staticMaterial.mainTexture = _staticTexture;
            _staticMaterial.mainTextureScale = new Vector2(3f, 2f);

            _staticRenderer = noise.AddComponent<MeshRenderer>();
            _staticRenderer.sharedMaterial = _staticMaterial;

            _noSignal = AssetFactory.CreateLabel(_screen, "NoSignal",
                new Vector3(0f, 0f, -0.002f), ModConfig.ScreenHeight * 0.13f,
                new Color(1f, 0.4f, 0.35f), TextAnchor.MiddleCenter);
            if (_noSignal != null)
            {
                _noSignal.text = string.Empty;
            }
        }

        /// <summary>Attaches the drone's render texture to the screen. Pass null to blank it.</summary>
        public void BindFeed(RenderTexture feed)
        {
            if (_screenMaterial == null)
            {
                return;
            }

            _screenMaterial.mainTexture = feed;
        }

        // ----------------------------------------------------------------- control

        public void SetDown(bool down)
        {
            if (_down == down)
            {
                return;
            }

            _down = down;
            ModLog.Trace("Goggles " + (down ? "down" : "up") + ".");
        }

        public void Toggle()
        {
            SetDown(!_down);
        }

        private void LateUpdate()
        {
            if (_head == null)
            {
                _head = GameBridge.Head;
                if (_head != null)
                {
                    transform.SetParent(_head, false);
                    transform.localPosition = Vector3.zero;
                    transform.localRotation = Quaternion.identity;
                }
            }

            float target = _down ? 0f : StowedAngle;
            _currentAngle = Mathf.Lerp(_currentAngle, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * FlipSpeed));
            ApplyAngle(_currentAngle);

            EnsureVisibleToPlayerCameras();
        }

        private void ApplyAngle(float angle)
        {
            if (_hinge != null)
            {
                _hinge.localRotation = Quaternion.Euler(angle, 0f, 0f);
            }
        }

        /// <summary>
        /// The goggles live on an otherwise-unused layer so the drone camera cannot
        /// see them. That also means the player's own cameras have to be told to
        /// render that layer -- and those cameras are created and swapped by the game,
        /// so this is rechecked periodically rather than once.
        /// </summary>
        private void EnsureVisibleToPlayerCameras()
        {
            if (Time.unscaledTime - _cameraRefresh < 2f)
            {
                return;
            }

            _cameraRefresh = Time.unscaledTime;

            int layer = LayerUtil.GoggleLayer;
            if (layer < 0)
            {
                return;
            }

            int mask = 1 << layer;
            Camera[] cameras = Camera.allCameras;

            for (int i = 0; i < cameras.Length; i++)
            {
                Camera camera = cameras[i];
                if (camera == null || camera.targetTexture != null)
                {
                    // Anything rendering to a texture is someone else's feed, including ours.
                    continue;
                }

                if ((camera.cullingMask & mask) == 0)
                {
                    camera.cullingMask |= mask;
                }
            }
        }

        // ------------------------------------------------------------------ display

        /// <summary>
        /// Drives the picture: tint for the camera mode, breakup and dropout for the
        /// link, and the OSD. Called every frame by the manager.
        /// </summary>
        public void Refresh(DroneController drone)
        {
            bool showFeed = _down && drone != null && drone.IsAlive;

            if (_screenRenderer != null)
            {
                _screenRenderer.enabled = _down;
            }

            _osd.SetVisible(_down && ModConfig.ShowOsd);

            if (!_down)
            {
                if (_staticRenderer != null)
                {
                    _staticRenderer.enabled = false;
                }

                return;
            }

            float quality = 1f;
            bool lost = true;

            if (drone != null && drone.Link != null)
            {
                quality = drone.Link.Quality;
                lost = drone.Link.IsLost;
            }

            if (drone == null || !drone.IsAlive)
            {
                quality = 0f;
                lost = true;
            }

            // Camera-mode tint, dimmed as the signal falls away.
            Color tint = drone != null && drone.Camera != null ? drone.Camera.ModeTint : Color.white;
            float brightness = Mathf.Lerp(0.25f, 1f, Mathf.Clamp01(quality * 1.3f));
            if (_screenMaterial != null)
            {
                _screenMaterial.color = new Color(tint.r * brightness, tint.g * brightness, tint.b * brightness, 1f);
            }

            UpdateStatic(quality, lost);

            if (_noSignal != null)
            {
                string message = string.Empty;
                if (drone == null || drone.State == DroneState.Stowed)
                {
                    message = "NO DRONE";
                }
                else if (drone.State == DroneState.Crashed)
                {
                    message = "SIGNAL LOST";
                }
                else if (lost)
                {
                    message = "NO SIGNAL";
                }

                if (!string.IsNullOrEmpty(message) && Mathf.Repeat(Time.time, 0.9f) > 0.55f)
                {
                    message = string.Empty;
                }

                _noSignal.text = message;
            }

            _osd.Refresh(showFeed ? drone : null);
        }

        private void UpdateStatic(float quality, bool lost)
        {
            if (_staticRenderer == null || _staticMaterial == null)
            {
                return;
            }

            if (!ModConfig.EnableStatic)
            {
                _staticRenderer.enabled = false;
                return;
            }

            // Analogue video holds up well until it does not: almost nothing below
            // about 75% quality, then a rapid collapse into full static.
            float breakup = Mathf.Clamp01(1f - quality / 0.75f);
            breakup = Mathf.Pow(breakup, 0.7f);
            if (lost)
            {
                breakup = 1f;
            }

            _staticRenderer.enabled = breakup > 0.01f;
            if (!_staticRenderer.enabled)
            {
                return;
            }

            _staticMaterial.color = new Color(1f, 1f, 1f, breakup * 0.92f);

            if (Time.unscaledTime - _lastStaticRefresh > StaticRefreshInterval)
            {
                _lastStaticRefresh = Time.unscaledTime;
                AssetFactory.RandomiseStatic(_staticTexture);

                // Scroll the noise so it does not read as a fixed pattern.
                _staticMaterial.mainTextureOffset = new Vector2(Random.value, Random.value);
            }
        }

        private void OnDestroy()
        {
            if (_staticTexture != null)
            {
                Object.Destroy(_staticTexture);
            }
        }
    }
}
