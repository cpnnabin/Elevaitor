namespace Lift.Models
{
    public enum Direction { None, Up, Down }

    public enum ElevatorEventType
    {
        PanelRequest,        // button pressed INSIDE a lift
        RequestAssigned,     // hall (outside) request given to a lift by the dispatcher
        RequestRejected,
        MovementStarted,
        FloorChanged,        // display only - lift passed the half-way point to another floor
        FloorArrived,
        DoorsOpened,
        DoorsClosed,
        ObstructionDetected, // object in the doorway while the doors were closing
        EmergencyActivated,
        EmergencyReset,
        StateChanged         // display only - the lift moved to a new State object
    }

    public static class DirectionExtensions
    {
        public static string Arrow(this Direction d) =>
            d switch { Direction.Up => "▲", Direction.Down => "▼", _ => " " };

        public static Direction Opposite(this Direction d) =>
            d switch { Direction.Up => Direction.Down, Direction.Down => Direction.Up, _ => Direction.None };
    }
}
