using System.Data;

namespace Lift.Data
{
    public interface IElevatorLogRepository
    {
        void Save(IEnumerable<ElevatorLogEntry> entries);
        DataTable GetLogs();
        void ClearLogs();
    }

    public interface ILogWriter
    {
        void Write(ElevatorLogEntry entry);
    }
}
