namespace Lift.Models
{
    /// <summary>The message a Subject (Elevator) sends to every Observer.</summary>
    public sealed class ElevatorEvent
    {
        public ElevatorEvent(int elevatorId, string elevatorName, ElevatorEventType type,
            int currentFloor, int? fromFloor, int? toFloor, string message, DateTime timestamp)
        {
            ElevatorId = elevatorId;
            ElevatorName = elevatorName;
            Type = type;
            CurrentFloor = currentFloor;
            FromFloor = fromFloor;
            ToFloor = toFloor;
            Message = message;
            Timestamp = timestamp;
        }

        public int ElevatorId { get; }
        public string ElevatorName { get; }
        public ElevatorEventType Type { get; }
        public int CurrentFloor { get; }
        public int? FromFloor { get; }
        public int? ToFloor { get; }
        public string Message { get; }
        public DateTime Timestamp { get; }
    }
}
