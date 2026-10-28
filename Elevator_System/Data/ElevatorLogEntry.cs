namespace Lift.Data
{
    /// <summary>One row for the ElevatorLogs table. ElevatorId = null means a system message.</summary>
    public sealed record ElevatorLogEntry(
        DateTime Time,
        int? ElevatorId,
        string EventType,
        string Action,
        int? CurrentFloorId,
        int? FromFloorId,
        int? ToFloorId);
}
