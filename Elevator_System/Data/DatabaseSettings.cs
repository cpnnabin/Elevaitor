namespace Lift.Data
{
    public static class DatabaseSettings
    {
        // LocalDB instance + database name. No file path is used, so the project works on any PC
        // that has SQL Server LocalDB (it ships with Visual Studio).
        public const string DatabaseName = "Elevator";

        public const string ConnectionString =
            @"Server=(localdb)\MSSQLLocalDB;Database=Elevator;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=10";

        // The setup script is copied next to the .exe and found with a RELATIVE path.
        public static readonly string[] ScriptRelativePath = { "Database", "Elevator.sql" };
    }
}
