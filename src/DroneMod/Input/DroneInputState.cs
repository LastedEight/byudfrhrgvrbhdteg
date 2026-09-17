namespace DroneMod.Input
{
    /// <summary>
    /// One frame of pilot demand, normalised and source-agnostic. The flight model
    /// consumes only this, which is what lets the autopilot fly the drone by
    /// synthesising the same structure.
    /// </summary>
    internal struct DroneInputState
    {
        /// <summary>Throttle stick position, -1 (full down) to +1 (full up). Self-centring.</summary>
        public float ThrottleStick;

        /// <summary>Integrated throttle, 0 to 1. Used in acro mode where a self-centring stick is useless.</summary>
        public float ThrottleAbsolute;

        /// <summary>Positive pitches the nose up (stick back).</summary>
        public float Pitch;

        /// <summary>Positive rolls right.</summary>
        public float Roll;

        /// <summary>Positive yaws right.</summary>
        public float Yaw;

        /// <summary>Gimbal tilt rate demand, -1 (down) to +1 (up).</summary>
        public float GimbalPitch;

        /// <summary>Gimbal pan rate demand, -1 (left) to +1 (right).</summary>
        public float GimbalYaw;

        /// <summary>Zoom rate demand, -1 (out) to +1 (in).</summary>
        public float Zoom;

        /// <summary>True while the pilot is holding the gimbal modifier, so flight input is suppressed.</summary>
        public bool GimbalModifierHeld;

        public static DroneInputState Neutral
        {
            get
            {
                DroneInputState state = new DroneInputState();
                state.ThrottleAbsolute = 0f;
                return state;
            }
        }

        /// <summary>True when the pilot is asking for nothing, which is what arms position hold.</summary>
        public bool IsCentred(float epsilon)
        {
            return UnityEngine.Mathf.Abs(Pitch) < epsilon
                && UnityEngine.Mathf.Abs(Roll) < epsilon
                && UnityEngine.Mathf.Abs(Yaw) < epsilon;
        }
    }
}
