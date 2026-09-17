using UnityEngine;

namespace DroneMod.Drone
{
    /// <summary>
    /// Spins a propeller. Purely cosmetic -- lift comes from the flight model, not
    /// from these -- but the visual and audio cue of the props spooling is most of
    /// what sells the drone as a machine rather than a floating camera.
    /// </summary>
    internal sealed class PropSpinner : MonoBehaviour
    {
        /// <summary>1 for a clockwise prop, -1 for counter-clockwise. Diagonal pairs match, as on a real quad.</summary>
        public float Direction = 1f;

        /// <summary>Commanded speed, 0 to 1.</summary>
        public float Speed;

        private const float MaxRpm = 28000f;
        private float _current;

        private void Update()
        {
            // Motors have inertia; they do not snap to a new speed.
            _current = Mathf.MoveTowards(_current, Mathf.Clamp01(Speed), Time.deltaTime * 3.5f);

            float degreesPerSecond = _current * MaxRpm / 60f * 360f;
            transform.Rotate(0f, degreesPerSecond * Time.deltaTime * Direction, 0f, Space.Self);
        }
    }
}
