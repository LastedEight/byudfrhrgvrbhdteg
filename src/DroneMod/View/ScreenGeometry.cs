using UnityEngine;
using DroneMod.Config;

namespace DroneMod.View
{
    /// <summary>
    /// The dimensions the goggle screen mesh is generated from. Kept as a value so
    /// the manager can tell, in one comparison, whether a settings change needs the
    /// goggles rebuilt or merely re-bound.
    /// </summary>
    internal struct ScreenGeometry
    {
        public float Width;
        public float Height;
        public float Curvature;
        public float Distance;

        public static ScreenGeometry FromConfig()
        {
            ScreenGeometry geometry;
            geometry.Width = ModConfig.ScreenWidth;
            geometry.Height = ModConfig.ScreenHeight;
            geometry.Curvature = ModConfig.ScreenCurvature;
            geometry.Distance = ModConfig.ScreenDistance;
            return geometry;
        }

        public bool Matches(ScreenGeometry other)
        {
            return Mathf.Abs(Width - other.Width) < 0.0005f
                && Mathf.Abs(Height - other.Height) < 0.0005f
                && Mathf.Abs(Curvature - other.Curvature) < 0.005f
                && Mathf.Abs(Distance - other.Distance) < 0.0005f;
        }
    }
}
