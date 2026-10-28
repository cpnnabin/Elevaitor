using Lift.Data;
using Lift.Models;

namespace Lift.Observer
{
    /// <summary>Turns lift events into log rows. It does not know about the database - only about ILogWriter.</summary>
    public sealed class LogObserver : IElevatorObserver
    {
        private readonly ILogWriter _writer;

        public LogObserver(ILogWriter writer) => _writer = writer;

        public void Update(ElevatorEvent e)
        {
            // display-only events are not operation records
            if (e.Type == ElevatorEventType.FloorChanged || e.Type == ElevatorEventType.StateChanged) return;

            _writer.Write(new ElevatorLogEntry(
                e.Timestamp, e.ElevatorId, e.Type.ToString(), e.Message,
                e.CurrentFloor, e.FromFloor, e.ToFloor));
        }
    }
}
