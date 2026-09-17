using UnityEngine;
using DroneMod.Drone;
using DroneMod.Util;

namespace DroneMod.View
{
    /// <summary>
    /// The on-screen display drawn over the video feed: the same set of numbers a
    /// real FPV OSD shows, because those are the ones you actually need when the
    /// video is all you have.
    ///
    /// It is built from world-space text and quads parented to the goggle screen,
    /// not from a UI canvas. In VR a canvas is more trouble than it is worth, and
    /// geometry sitting a couple of millimetres in front of the screen composites
    /// correctly for both eyes for free.
    /// </summary>
    internal sealed class OsdRenderer
    {
        private const float Depth = -0.0015f;

        private Transform _root;
        private float _width;
        private float _height;

        private TextMesh _mode;
        private TextMesh _battery;
        private TextMesh _altitude;
        private TextMesh _speed;
        private TextMesh _home;
        private TextMesh _heading;
        private TextMesh _camera;
        private TextMesh _warning;
        private TextMesh _timer;

        private LineRenderer _horizon;
        private Transform _crosshair;
        private Transform[] _signalBars;

        private float _flightStarted;
        private float _warningUntil;
        private string _warningText = string.Empty;

        public void Build(Transform screen, float width, float height)
        {
            _width = width;
            _height = height;

            GameObject root = new GameObject("Osd");
            root.transform.SetParent(screen, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            _root = root.transform;

            float hw = width * 0.5f;
            float hh = height * 0.5f;
            float inset = width * 0.045f;
            float size = height * 0.075f;

            Color green = new Color(0.55f, 1f, 0.6f);
            Color white = new Color(0.92f, 0.96f, 1f);
            Color amber = new Color(1f, 0.78f, 0.25f);

            _mode = Label("Mode", new Vector3(-hw + inset, hh - inset, Depth), size, green, TextAnchor.UpperLeft);
            _camera = Label("CamMode", new Vector3(-hw + inset, hh - inset - size * 1.6f, Depth), size * 0.85f, green, TextAnchor.UpperLeft);
            _battery = Label("Battery", new Vector3(hw - inset, hh - inset, Depth), size, green, TextAnchor.UpperRight);
            _timer = Label("Timer", new Vector3(hw - inset, hh - inset - size * 1.6f, Depth), size * 0.85f, green, TextAnchor.UpperRight);
            _heading = Label("Heading", new Vector3(0f, hh - inset, Depth), size, white, TextAnchor.UpperCenter);
            _altitude = Label("Altitude", new Vector3(-hw + inset, -hh + inset, Depth), size, green, TextAnchor.LowerLeft);
            _speed = Label("Speed", new Vector3(hw - inset, -hh + inset, Depth), size, green, TextAnchor.LowerRight);
            _home = Label("Home", new Vector3(0f, -hh + inset, Depth), size * 0.9f, white, TextAnchor.LowerCenter);
            _warning = Label("Warning", new Vector3(0f, -hh * 0.42f, Depth), size * 1.15f, amber, TextAnchor.MiddleCenter);

            BuildCrosshair(green);
            BuildHorizon(green);
            BuildSignalBars(green, new Vector3(hw - inset - width * 0.10f, hh - inset - size * 0.35f, Depth));

            _flightStarted = Time.time;
        }

        private TextMesh Label(string name, Vector3 position, float characterSize, Color color, TextAnchor anchor)
        {
            // characterSize is in metres per font unit; the font is created at 48pt.
            // The render queue is set once on the shared font material, not here.
            return AssetFactory.CreateLabel(_root, name, position, characterSize / 48f * 1.6f, color, anchor);
        }

        private void BuildCrosshair(Color color)
        {
            GameObject crosshair = new GameObject("Crosshair");
            crosshair.transform.SetParent(_root, false);
            crosshair.transform.localPosition = new Vector3(0f, 0f, Depth);
            _crosshair = crosshair.transform;

            Material material = AssetFactory.CreateScreenMaterial(color, 3200);
            float arm = _width * 0.028f;
            float thickness = _height * 0.006f;

            AssetFactory.CreateVisualPrimitive(PrimitiveType.Cube, _crosshair, "H",
                new Vector3(-arm * 1.6f, 0f, 0f), Quaternion.identity,
                new Vector3(arm, thickness, thickness), material);
            AssetFactory.CreateVisualPrimitive(PrimitiveType.Cube, _crosshair, "H2",
                new Vector3(arm * 1.6f, 0f, 0f), Quaternion.identity,
                new Vector3(arm, thickness, thickness), material);
            AssetFactory.CreateVisualPrimitive(PrimitiveType.Cube, _crosshair, "C",
                Vector3.zero, Quaternion.identity,
                new Vector3(thickness, thickness, thickness), material);
        }

        private void BuildHorizon(Color color)
        {
            GameObject horizon = new GameObject("Horizon");
            horizon.transform.SetParent(_root, false);
            horizon.transform.localPosition = new Vector3(0f, 0f, Depth * 0.8f);

            _horizon = horizon.AddComponent<LineRenderer>();
            _horizon.useWorldSpace = false;
            _horizon.positionCount = 4;
            _horizon.startWidth = _height * 0.008f;
            _horizon.endWidth = _height * 0.008f;
            _horizon.material = AssetFactory.CreateScreenMaterial(new Color(color.r, color.g, color.b, 0.75f), 3190);
        }

        private void BuildSignalBars(Color color, Vector3 origin)
        {
            _signalBars = new Transform[4];
            Material material = AssetFactory.CreateScreenMaterial(color, 3200);

            float barWidth = _width * 0.008f;
            float spacing = _width * 0.013f;

            for (int i = 0; i < 4; i++)
            {
                float barHeight = _height * (0.022f + 0.014f * i);
                GameObject bar = AssetFactory.CreateVisualPrimitive(
                    PrimitiveType.Cube, _root, "SignalBar" + i,
                    origin + new Vector3(i * spacing, barHeight * 0.5f, 0f), Quaternion.identity,
                    new Vector3(barWidth, barHeight, barWidth * 0.5f), material);
                _signalBars[i] = bar.transform;
            }
        }

        /// <summary>Shows a transient message in the middle of the feed, e.g. a failsafe announcement.</summary>
        public void ShowWarning(string message, float seconds)
        {
            _warningText = message;
            _warningUntil = Time.time + seconds;
        }

        public void ResetFlightTimer()
        {
            _flightStarted = Time.time;
        }

        public void SetVisible(bool visible)
        {
            if (_root != null)
            {
                _root.gameObject.SetActive(visible);
            }
        }

        /// <summary>Refreshes every readout. Cheap enough to run each frame.</summary>
        public void Refresh(DroneController drone)
        {
            if (_root == null)
            {
                return;
            }

            if (drone == null)
            {
                Set(_mode, "NO DRONE");
                Set(_battery, string.Empty);
                Set(_altitude, string.Empty);
                Set(_speed, string.Empty);
                Set(_home, string.Empty);
                Set(_heading, string.Empty);
                Set(_camera, string.Empty);
                Set(_timer, string.Empty);
                Set(_warning, string.Empty);
                return;
            }

            Set(_mode, drone.ModeLabel);

            DroneBattery battery = drone.Battery;
            if (battery != null)
            {
                string batteryText = Mathf.RoundToInt(battery.Charge * 100f) + "%  " + battery.Voltage.ToString("0.0") + "V";
                Set(_battery, batteryText);
                if (_battery != null)
                {
                    _battery.color = battery.IsCritical
                        ? new Color(1f, 0.35f, 0.3f)
                        : (battery.IsLow ? new Color(1f, 0.78f, 0.25f) : new Color(0.55f, 1f, 0.6f));
                }
            }

            Set(_altitude, "ALT " + Mathf.RoundToInt(drone.AltitudeAgl) + "m");
            Set(_speed, Mathf.RoundToInt(drone.GroundSpeed * 3.6f) + "km/h");
            Set(_heading, Mathf.RoundToInt(drone.Heading).ToString("000"));

            DroneLink link = drone.Link;
            float distance = drone.DistanceToHome;
            Set(_home, "HOME " + MathUtil.FormatDistance(distance) + "   " + BearingArrow(drone) + "   RX " + MathUtil.FormatDistance(link != null ? link.Distance : 0f));

            DroneCameraRig camera = drone.Camera;
            if (camera != null)
            {
                Set(_camera, camera.ModeLabel + "  x" + camera.ZoomFactor.ToString("0.0") + "  " + Mathf.RoundToInt(camera.GimbalPitch) + "deg");
            }

            Set(_timer, MathUtil.FormatClock(Time.time - _flightStarted));

            UpdateHorizon(drone);
            UpdateSignalBars(link);
            UpdateWarning(drone);
        }

        private static string BearingArrow(DroneController drone)
        {
            Vector3 toHome = drone.HomePoint - drone.transform.position;
            float relative = MathUtil.WrapAngle(MathUtil.HeadingOf(toHome) - drone.Heading);

            if (relative > -22.5f && relative <= 22.5f) return "^";
            if (relative > 22.5f && relative <= 67.5f) return "/";
            if (relative > 67.5f && relative <= 112.5f) return ">";
            if (relative > 112.5f && relative <= 157.5f) return "\\";
            if (relative > -67.5f && relative <= -22.5f) return "\\";
            if (relative > -112.5f && relative <= -67.5f) return "<";
            if (relative > -157.5f && relative <= -112.5f) return "/";
            return "v";
        }

        /// <summary>
        /// The artificial horizon: two short segments that roll opposite the airframe
        /// and slide with pitch, exactly as a Betaflight OSD draws it.
        /// </summary>
        private void UpdateHorizon(DroneController drone)
        {
            if (_horizon == null)
            {
                return;
            }

            float roll = -Mathf.Asin(Mathf.Clamp(drone.transform.right.y, -1f, 1f)) * Mathf.Rad2Deg;
            float pitch = MathUtil.PitchOf(drone.transform.forward);

            float span = _width * 0.20f;
            float gap = _width * 0.055f;
            float verticalOffset = Mathf.Clamp(-pitch / 45f, -1f, 1f) * _height * 0.32f;

            float radians = -roll * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);

            _horizon.SetPosition(0, RotatePoint(-gap - span, cos, sin, verticalOffset));
            _horizon.SetPosition(1, RotatePoint(-gap, cos, sin, verticalOffset));
            _horizon.SetPosition(2, RotatePoint(gap, cos, sin, verticalOffset));
            _horizon.SetPosition(3, RotatePoint(gap + span, cos, sin, verticalOffset));
        }

        /// <summary>Rotates a point on the horizontal axis by the bank angle and slides it for pitch.</summary>
        private static Vector3 RotatePoint(float x, float cos, float sin, float verticalOffset)
        {
            return new Vector3(x * cos, x * sin + verticalOffset, 0f);
        }

        private void UpdateSignalBars(DroneLink link)
        {
            if (_signalBars == null)
            {
                return;
            }

            int bars = link != null ? link.Bars : 0;
            for (int i = 0; i < _signalBars.Length; i++)
            {
                if (_signalBars[i] == null)
                {
                    continue;
                }

                MeshRenderer renderer = _signalBars[i].GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.enabled = i < bars;
                }
            }
        }

        private void UpdateWarning(DroneController drone)
        {
            string text = string.Empty;

            if (Time.time < _warningUntil)
            {
                text = _warningText;
            }
            else if (drone.State == DroneState.Crashed)
            {
                text = "DRONE LOST";
            }
            else if (drone.Battery != null && drone.Battery.IsCritical)
            {
                text = "BATTERY CRITICAL";
            }
            else if (drone.Link != null && drone.Link.Quality < 0.25f)
            {
                text = "LOW SIGNAL";
            }
            else if (drone.Battery != null && drone.Battery.IsLow)
            {
                text = "BATTERY LOW  " + MathUtil.FormatClock(drone.Battery.EstimatedSecondsRemaining);
            }

            // Blink, so it reads as a warning rather than as another readout.
            if (!string.IsNullOrEmpty(text) && Mathf.Repeat(Time.time, 1f) > 0.6f)
            {
                text = string.Empty;
            }

            Set(_warning, text);
        }

        private static void Set(TextMesh target, string value)
        {
            if (target != null && target.text != value)
            {
                target.text = value;
            }
        }
    }
}
