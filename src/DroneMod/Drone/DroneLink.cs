using UnityEngine;
using DroneMod.Config;
using DroneMod.Core;
using DroneMod.Util;

namespace DroneMod.Drone
{
    /// <summary>
    /// The radio link between the drone and the goggles.
    ///
    /// This is what makes the mod interesting rather than just a free camera: range
    /// costs you picture quality, terrain between you and the drone costs you a lot
    /// more, and losing the link entirely hands the drone to the failsafe. The
    /// quality figure drives both the video static and the control latency.
    /// </summary>
    internal sealed class DroneLink : MonoBehaviour
    {
        private const float LosCheckInterval = 0.25f;

        private Transform _pilot;
        private Transform _droneRoot;
        private float _rawQuality = 1f;
        private float _lastLosCheck = -999f;
        private float _losFactor = 1f;
        private float _lostFor;

        /// <summary>Smoothed link quality, 1 (perfect) to 0 (no signal).</summary>
        public float Quality { get; private set; }

        /// <summary>Straight-line distance from the pilot to the drone, metres.</summary>
        public float Distance { get; private set; }

        /// <summary>True when the picture and control have been gone long enough to trigger the failsafe.</summary>
        public bool IsLost
        {
            get { return _lostFor > 1.5f; }
        }

        /// <summary>True when the link is degraded enough to show visible breakup.</summary>
        public bool IsDegraded
        {
            get { return Quality < 0.75f; }
        }

        /// <summary>Round-trip control latency in seconds. Small, but it grows as the link degrades.</summary>
        public float Latency
        {
            get { return Mathf.Lerp(0.18f, 0.02f, Quality); }
        }

        public void Initialise(Transform droneRoot)
        {
            _droneRoot = droneRoot;
            Quality = 1f;
            _rawQuality = 1f;
            _lostFor = 0f;
        }

        private void Update()
        {
            if (_droneRoot == null)
            {
                return;
            }

            if (_pilot == null)
            {
                _pilot = GameBridge.Head;
            }

            Vector3 pilotPosition = _pilot != null ? _pilot.position : _droneRoot.position;
            Distance = Vector3.Distance(pilotPosition, _droneRoot.position);

            float range = Mathf.Max(50f, ModConfig.LinkRange);

            // Inverse-square-ish falloff normalised so quality is 1 at the pilot and
            // reaches 0 a little past the configured range.
            float normalised = Distance / range;
            float rangeFactor = Mathf.Clamp01(1f - normalised * normalised * 0.85f - normalised * 0.15f);

            if (Time.time - _lastLosCheck > LosCheckInterval)
            {
                _lastLosCheck = Time.time;
                _losFactor = CheckLineOfSight(pilotPosition, _droneRoot.position) ? 1f : 0.22f;
            }

            // Antennas are not isotropic: pointing the drone's tail at the pilot costs a little.
            float aspect = Vector3.Dot(_droneRoot.up, Vector3.up);
            float attitudeFactor = Mathf.Lerp(0.75f, 1f, Mathf.Clamp01(Mathf.Abs(aspect)));

            _rawQuality = Mathf.Clamp01(rangeFactor * _losFactor * attitudeFactor);

            // Multipath scintillation: a real link never sits perfectly still.
            float shimmer = (Mathf.PerlinNoise(Time.time * 1.7f, 0.5f) - 0.5f) * 0.08f * (1f - _rawQuality);
            Quality = Mathf.Clamp01(MathUtil.Smooth(Quality, _rawQuality + shimmer, 4f, Time.deltaTime));

            if (Quality < 0.06f)
            {
                _lostFor += Time.deltaTime;
            }
            else
            {
                _lostFor = 0f;
            }
        }

        /// <summary>
        /// One ray from pilot to drone, skipping anything that belongs to the drone
        /// or to whatever the pilot is sitting in.
        /// </summary>
        private bool CheckLineOfSight(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            float distance = delta.magnitude;
            if (distance < 1f)
            {
                return true;
            }

            RaycastHit[] hits = Physics.RaycastAll(from, delta / distance, distance, ~0, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
            {
                return true;
            }

            Transform vehicle = null;
            GameObject playerVehicle = GameBridge.PlayerVehicle;
            if (playerVehicle != null)
            {
                vehicle = playerVehicle.transform;
            }

            for (int i = 0; i < hits.Length; i++)
            {
                Transform hit = hits[i].transform;
                if (hit == null)
                {
                    continue;
                }

                if (_droneRoot != null && hit.IsChildOf(_droneRoot))
                {
                    continue;
                }

                if (vehicle != null && hit.IsChildOf(vehicle))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        /// <summary>Four-bar signal strength for the OSD.</summary>
        public int Bars
        {
            get { return Mathf.Clamp(Mathf.CeilToInt(Quality * 4f), 0, 4); }
        }
    }
}
