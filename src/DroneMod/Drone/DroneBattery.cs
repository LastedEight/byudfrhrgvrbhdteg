using UnityEngine;
using DroneMod.Config;
using DroneMod.Util;

namespace DroneMod.Drone
{
    /// <summary>
    /// Battery model. Endurance is expressed as hover-seconds and scaled by actual
    /// power draw, so hovering gently lasts the advertised time and full-throttle
    /// climbs eat it far faster. Voltage sag is modelled well enough to drive the
    /// OSD and the low-battery failsafe.
    /// </summary>
    internal sealed class DroneBattery : MonoBehaviour
    {
        private const float CellCount = 6f;
        private const float FullCellVolts = 4.2f;
        private const float EmptyCellVolts = 3.3f;

        /// <summary>Remaining charge, 1 to 0.</summary>
        public float Charge { get; private set; }

        /// <summary>Instantaneous draw as a multiple of hover draw.</summary>
        public float LoadFactor { get; private set; }

        /// <summary>Pack voltage, including sag under load.</summary>
        public float Voltage { get; private set; }

        public bool IsLow
        {
            get { return Charge <= 0.20f; }
        }

        public bool IsCritical
        {
            get { return Charge <= 0.08f; }
        }

        public bool IsFlat
        {
            get { return Charge <= 0.0005f; }
        }

        /// <summary>Estimated flight time left at the current draw, seconds.</summary>
        public float EstimatedSecondsRemaining
        {
            get
            {
                float load = Mathf.Max(0.15f, LoadFactor);
                return Charge * ModConfig.BatterySeconds / load;
            }
        }

        private void Awake()
        {
            Charge = 1f;
            LoadFactor = 1f;
            Voltage = CellCount * FullCellVolts;
        }

        public void Recharge()
        {
            Charge = 1f;
            LoadFactor = 1f;
            Voltage = CellCount * FullCellVolts;
        }

        /// <summary>
        /// Called every physics step by the flight model.
        /// <paramref name="thrustFraction"/> is commanded thrust over hover thrust.
        /// </summary>
        public void Consume(float thrustFraction, float deltaTime)
        {
            if (Charge <= 0f)
            {
                Charge = 0f;
                Voltage = CellCount * EmptyCellVolts;
                return;
            }

            // Electrical power goes roughly as thrust^1.5 for a propeller, plus a
            // fixed avionics/video-transmitter draw that never goes away.
            float mechanical = Mathf.Pow(Mathf.Max(0f, thrustFraction), 1.5f);
            LoadFactor = MathUtil.Smooth(LoadFactor, 0.12f + mechanical * 0.88f, 6f, deltaTime);

            float drainPerSecond = LoadFactor / Mathf.Max(1f, ModConfig.BatterySeconds);
            Charge = Mathf.Clamp01(Charge - drainPerSecond * deltaTime);

            float restingVolts = Mathf.Lerp(EmptyCellVolts, FullCellVolts, Mathf.Pow(Charge, 0.55f));
            float sag = 0.22f * LoadFactor;
            Voltage = CellCount * Mathf.Max(3.0f, restingVolts - sag);
        }
    }
}
