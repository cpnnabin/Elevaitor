using System.Data;
using Microsoft.Data.SqlClient;

namespace Lift.Data
{
    /// <summary>
    /// Disconnected model: every change goes through SqlDataAdapter.Update() on a DataTable.
    /// There is no ExecuteNonQuery() in the data-changing code.
    /// </summary>
    public sealed class SqlElevatorLogRepository : IElevatorLogRepository
    {
        private const string DetailSql =
            @"SELECT LogId, LogDate, LogClock, ElevatorName, EventType, CurrentFloor, FromFloor, ToFloor, Action
              FROM dbo.vw_ElevatorLogDetail ORDER BY LogId DESC";

        private readonly string _connectionString;
        private readonly object _initLock = new();
        private bool _ready;

        public SqlElevatorLogRepository(string connectionString = DatabaseSettings.ConnectionString)
        {
            _connectionString = connectionString;
        }

        public void Save(IEnumerable<ElevatorLogEntry> entries)
        {
            ModifyTable("SELECT TOP 0 * FROM dbo.ElevatorLogs", table =>
            {
                foreach (ElevatorLogEntry entry in entries)
                {
                    DataRow row = table.NewRow();
                    row["LogTime"] = entry.Time;
                    row["ElevatorId"] = (object?)entry.ElevatorId ?? DBNull.Value;
                    row["EventType"] = entry.EventType;
                    row["Action"] = entry.Action;
                    row["CurrentFloorId"] = (object?)entry.CurrentFloorId ?? DBNull.Value;
                    row["FromFloorId"] = (object?)entry.FromFloorId ?? DBNull.Value;
                    row["ToFloorId"] = (object?)entry.ToFloorId ?? DBNull.Value;
                    table.Rows.Add(row);
                }
            });
        }

        public DataTable GetLogs()
        {
            EnsureReady();
            var table = new DataTable("ElevatorLogDetail");
            using var connection = new SqlConnection(_connectionString);
            using var adapter = new SqlDataAdapter(DetailSql, connection);
            adapter.Fill(table);
            return table;
        }

        public void ClearLogs()
        {
            ModifyTable("SELECT * FROM dbo.ElevatorLogs", table =>
            {
                foreach (DataRow row in table.Rows) row.Delete();
            });
        }

        // The single method that fills a DataTable, lets the caller change it, and saves it with Update().
        private void ModifyTable(string selectSql, Action<DataTable> change)
        {
            EnsureReady();
            var table = new DataTable();
            using var connection = new SqlConnection(_connectionString);
            using var adapter = new SqlDataAdapter(selectSql, connection);
            using var builder = new SqlCommandBuilder(adapter) { ConflictOption = ConflictOption.OverwriteChanges };

            adapter.Fill(table);
            change(table);
            adapter.Update(table);
        }

        private void EnsureReady()
        {
            if (_ready) return;
            lock (_initLock)
            {
                if (_ready) return;
                DatabaseInitializer.EnsureCreated(_connectionString);
                _ready = true;
            }
        }
    }
}
