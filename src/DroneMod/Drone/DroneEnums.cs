namespace DroneMod.Drone
{
    internal enum FlightMode
    {
        /// <summary>Rate mode. The sticks command angular rates and nothing self-levels. Hardest, most fun.</summary>
        Acro = 0,

        /// <summary>Angle mode. Sticks command bank and pitch angle; centring the sticks levels the drone.</summary>
        Stabilised = 1,

        /// <summary>Angle mode plus barometric altitude hold on a centred throttle.</summary>
        AltitudeHold = 2,

        /// <summary>Altitude hold plus horizontal position hold. Let go and it parks.</summary>
        PositionHold = 3
    }

    internal enum AutoMode
    {
        Off = 0,
        ReturnHome = 1,
        Land = 2
    }

    internal enum CameraMode
    {
        Daylight = 0,
        LowLight = 1,
        Thermal = 2
    }

    internal enum DroneState
    {
        Stowed = 0,
        Flying = 1,
        Landed = 2,
        Crashed = 3
    }
}
