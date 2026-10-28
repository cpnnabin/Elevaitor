namespace Lift.Models
{
    /// <summary>Floor details in one place. FloorId values match the Floors table in the database.</summary>
    public static class FloorInfo
    {
        public const int Ground = 1;
        public const int First = 2;

        public const int MinFloor = Ground;
        public const int MaxFloor = First;

        public static bool IsValid(int floor) => floor >= MinFloor && floor <= MaxFloor;

        public static string Name(int floor) => floor switch
        {
            1 => "Ground Floor",
            2 => "First Floor",
            _ => $"Floor {floor}"
        };

        /// <summary>Short text shown on displays and buttons (G, 1).</summary>
        public static string Label(int floor) => floor switch
        {
            1 => "G",
            2 => "1",
            _ => floor.ToString()
        };
    }
}
