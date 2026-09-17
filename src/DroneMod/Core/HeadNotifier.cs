using UnityEngine;
using DroneMod.Util;

namespace DroneMod.Core
{
    /// <summary>
    /// Short status messages shown just below the pilot's line of sight, for the
    /// times when the goggles are up and the OSD is not visible -- "drone deployed",
    /// "airspeed too high", and so on.
    /// </summary>
    internal sealed class HeadNotifier : MonoBehaviour
    {
        private const float FadeSeconds = 0.6f;

        private TextMesh _text;
        private Transform _head;
        private float _expiresAt;

        public void Build(Transform head)
        {
            _head = head;

            transform.SetParent(head, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            _text = AssetFactory.CreateLabel(
                transform, "Notification",
                new Vector3(0f, -0.13f, 0.75f),
                0.022f,   // ~1.7 degrees tall at 0.75 m, which reads comfortably in a headset.
                new Color(0.85f, 0.95f, 1f),
                TextAnchor.MiddleCenter);

            LayerUtil.SetLayerRecursively(transform, LayerUtil.GoggleLayer);
        }

        /// <summary>Hidden while the goggles are down; the OSD carries messages there instead.</summary>
        public void SetVisible(bool visible)
        {
            if (_text != null)
            {
                _text.gameObject.SetActive(visible);
            }
        }

        public void Show(string message, float seconds)
        {
            ModLog.Info(message);

            if (_text == null)
            {
                return;
            }

            _text.text = message;
            _expiresAt = Time.unscaledTime + seconds;
        }

        private void LateUpdate()
        {
            if (_text == null)
            {
                return;
            }

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

            float remaining = _expiresAt - Time.unscaledTime;
            if (remaining <= 0f)
            {
                if (_text.text.Length > 0)
                {
                    _text.text = string.Empty;
                }

                return;
            }

            Color color = _text.color;
            color.a = Mathf.Clamp01(remaining / FadeSeconds);
            _text.color = color;
        }
    }
}
