using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace Lift.Data
{
    /// <summary>Creates the Elevator database and tables from Database/Elevator.sql if they are missing.</summary>
    public static class DatabaseInitializer
    {
        public static void EnsureCreated(string connectionString)
        {
            string path = Path.Combine(new[] { AppContext.BaseDirectory }.Concat(DatabaseSettings.ScriptRelativePath).ToArray());
            if (!File.Exists(path))
                throw new FileNotFoundException("Database setup script not found: " + path, path);

            // Connect to 'master' first, because the Elevator database may not exist yet.
            var builder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
            string script = File.ReadAllText(path);

            using var connection = new SqlConnection(builder.ConnectionString);
            connection.Open();

            foreach (string batch in Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(batch)) continue;
                using var command = new SqlCommand(batch, connection);
                command.ExecuteNonQuery();
            }
        }
    }
}
