using UnityEngine;
using DroneMod.Config;
using DroneMod.Core;
using DroneMod.Util;

namespace DroneMod.Input
{
    /// <summary>
    /// Turns VR thumbsticks (or the keyboard, for testing on a flat screen) into a
    /// <see cref="DroneInputState"/>.
    ///
    /// This deliberately reads the hand controllers directly rather than the
    /// aircraft's stick and throttle: the point of the mod is that the drone is
    /// flown independently of the jet, so the pilot lets go of the aircraft
    /// controls and flies the quad with both thumbs, exactly like a real
    /// transmitter.
    /// </summary>
    internal sealed class DroneInput
    {
        private const float ThrottleIntegrationRate = 0.9f;

        private float _integratedThrottle = 0f;
        private bool _vrWarned;

        /// <summary>True when at least one VR controller answered on the last poll.</summary>
        public bool UsingVrControllers { get; private set; }

        public void ResetThrottle(float value)
        {
            _integratedThrottle = Mathf.Clamp01(value);
        }

        public DroneInputState Poll(float deltaTime)
        {
            Vector2 leftStick;
            Vector2 rightStick;

            bool hasLeft = GameBridge.TryGetThumbstick(false, out leftStick);
            bool hasRight = GameBridge.TryGetThumbstick(true, out rightStick);
            UsingVrControllers = hasLeft || hasRight;

            if (!UsingVrControllers && !_vrWarned)
            {
                _vrWarned = true;
                ModLog.Info("No VR thumbstick input available; using the keyboard fallback bindings.");
            }

            leftStick = Shape(leftStick);
            rightStick = Shape(rightStick);

            // Keyboard fallback is additive, so a partially working controller still helps.
            Vector2 keyLeft = new Vector2(
                Axis(ModConfig.YawRightKey, ModConfig.YawLeftKey),
                Axis(ModConfig.ThrottleUpKey, ModConfig.ThrottleDownKey));
            Vector2 keyRight = new Vector2(
                Axis(ModConfig.RollRightKey, ModConfig.RollLeftKey),
                Axis(ModConfig.PitchBackKey, ModConfig.PitchForwardKey));

            leftStick = ClampStick(leftStick + keyLeft);
            rightStick = ClampStick(rightStick + keyRight);

            float gripValue;
            bool gimbalModifier = GameBridge.TryGetGrip(true, out gripValue) && gripValue > 0.6f;

            DroneInputState state = new DroneInputState();
            state.GimbalModifierHeld = gimbalModifier;

            // Mode 2: throttle and yaw on the left, pitch and roll on the right.
            // Mode 1 swaps the two vertical axes.
            Vector2 flightRight = gimbalModifier ? Vector2.zero : rightStick;

            if (ModConfig.Mode2Sticks)
            {
                state.ThrottleStick = leftStick.y;
                state.Yaw = leftStick.x;
                state.Pitch = flightRight.y;
                state.Roll = flightRight.x;
            }
            else
            {
                state.ThrottleStick = flightRight.y;
                state.Yaw = leftStick.x;
                state.Pitch = leftStick.y;
                state.Roll = flightRight.x;
            }

            // The camera rides on the same right stick, gated behind the grip.
            if (gimbalModifier)
            {
                state.GimbalPitch = rightStick.y;
                state.GimbalYaw = rightStick.x;
            }

            state.GimbalPitch += Axis(ModConfig.GimbalUpKey, ModConfig.GimbalDownKey);
            state.GimbalPitch = Mathf.Clamp(state.GimbalPitch, -1f, 1f);
            state.Zoom = Mathf.Clamp(Axis(ModConfig.ZoomInKey, ModConfig.ZoomOutKey), -1f, 1f);

            _integratedThrottle = Mathf.Clamp01(_integratedThrottle + state.ThrottleStick * ThrottleIntegrationRate * deltaTime);
            state.ThrottleAbsolute = _integratedThrottle;

            return state;
        }

        private static Vector2 Shape(Vector2 raw)
        {
            float x = MathUtil.Expo(MathUtil.Deadzone(raw.x, ModConfig.StickDeadzone), ModConfig.StickExpo);
            float y = MathUtil.Expo(MathUtil.Deadzone(raw.y, ModConfig.StickDeadzone), ModConfig.StickExpo);
            return new Vector2(x, y);
        }

        private static Vector2 ClampStick(Vector2 v)
        {
            return new Vector2(Mathf.Clamp(v.x, -1f, 1f), Mathf.Clamp(v.y, -1f, 1f));
        }

        private static float Axis(KeyCode positive, KeyCode negative)
        {
            float value = 0f;
            if (UnityEngine.Input.GetKey(positive))
            {
                value += 1f;
            }

            if (UnityEngine.Input.GetKey(negative))
            {
                value -= 1f;
            }

            return value;
        }
    }
}
