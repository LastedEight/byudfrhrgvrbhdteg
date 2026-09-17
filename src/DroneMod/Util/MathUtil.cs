using UnityEngine;

namespace DroneMod.Util
{
    internal static class MathUtil
    {
        /// <summary>Removes a centre deadzone and rescales the remainder back to the full [-1, 1] range.</summary>
        public static float Deadzone(float value, float deadzone)
        {
            float magnitude = Mathf.Abs(value);
            if (magnitude <= deadzone)
            {
                return 0f;
            }

            float scaled = (magnitude - deadzone) / Mathf.Max(0.0001f, 1f - deadzone);
            return Mathf.Sign(value) * Mathf.Clamp01(scaled);
        }

        /// <summary>
        /// Standard RC transmitter expo curve. <paramref name="expo"/> 0 is linear,
        /// 1 is fully cubic (soft around centre, full authority at the stops).
        /// </summary>
        public static float Expo(float value, float expo)
        {
            float e = Mathf.Clamp01(expo);
            return value * ((1f - e) + e * value * value);
        }

        /// <summary>Wraps any angle into (-180, 180].</summary>
        public static float WrapAngle(float degrees)
        {
            degrees %= 360f;
            if (degrees > 180f)
            {
                degrees -= 360f;
            }
            else if (degrees <= -180f)
            {
                degrees += 360f;
            }

            return degrees;
        }

        /// <summary>Signed pitch of a direction vector above the horizon, in degrees.</summary>
        public static float PitchOf(Vector3 direction)
        {
            return Mathf.Asin(Mathf.Clamp(direction.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
        }

        /// <summary>Compass heading (0 = north/+Z, 90 = east/+X) of a direction vector.</summary>
        public static float HeadingOf(Vector3 direction)
        {
            Vector3 flat = new Vector3(direction.x, 0f, direction.z);
            if (flat.sqrMagnitude < 0.000001f)
            {
                return 0f;
            }

            float heading = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
            return heading < 0f ? heading + 360f : heading;
        }

        /// <summary>Frame-rate independent exponential smoothing.</summary>
        public static float Smooth(float current, float target, float smoothingPerSecond, float deltaTime)
        {
            if (smoothingPerSecond <= 0f)
            {
                return target;
            }

            float t = 1f - Mathf.Exp(-deltaTime * smoothingPerSecond);
            return Mathf.Lerp(current, target, t);
        }

        public static Vector3 Smooth(Vector3 current, Vector3 target, float smoothingPerSecond, float deltaTime)
        {
            if (smoothingPerSecond <= 0f)
            {
                return target;
            }

            float t = 1f - Mathf.Exp(-deltaTime * smoothingPerSecond);
            return Vector3.Lerp(current, target, t);
        }

        /// <summary>Component-wise signed square, used for quadratic drag.</summary>
        public static Vector3 SignedSquare(Vector3 v)
        {
            return new Vector3(
                v.x * Mathf.Abs(v.x),
                v.y * Mathf.Abs(v.y),
                v.z * Mathf.Abs(v.z));
        }

        /// <summary>Formats seconds as m:ss, clamped at 0.</summary>
        public static string FormatClock(float seconds)
        {
            if (seconds < 0f || float.IsNaN(seconds) || float.IsInfinity(seconds))
            {
                seconds = 0f;
            }

            int total = Mathf.FloorToInt(seconds);
            return (total / 60).ToString() + ":" + (total % 60).ToString("00");
        }

        /// <summary>Formats a distance in metres, switching to kilometres past 1000 m.</summary>
        public static string FormatDistance(float metres)
        {
            if (metres >= 1000f)
            {
                return (metres / 1000f).ToString("0.00") + "km";
            }

            return Mathf.RoundToInt(metres).ToString() + "m";
        }
    }
}
