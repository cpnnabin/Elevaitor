namespace Lift.Models
{
    /// <summary>All timing constants. One "tick" = one WinForms timer tick (TickIntervalMs).</summary>
    public static class SimulationSettings
    {
        public const int TickIntervalMs = 30;
        public const double SpeedPerTick = 0.012;      // floors per tick  (about 2.5 s per floor)
        public const double DoorSpeedPerTick = 0.04;   // 25 ticks to open or close
        public const int DoorHoldTicks = 100;          // doors stay open about 3 s
        public const double Epsilon = 1e-6;

        public static double DoorCycleTicks => 1 / DoorSpeedPerTick + DoorHoldTicks + 1 / DoorSpeedPerTick;

        public static double TicksToSeconds(double ticks) => ticks * TickIntervalMs / 1000.0;
    }
}
