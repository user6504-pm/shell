using Spectre.Console;
using ParisShell.Services;
using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace ParisShell.Commands
{

    /// <summary>
    /// Command to display the content of a specific table in the database.
    /// Access depends on the user's role.
    /// </summary>
    internal class ShowTableCommand : ICommand
    {
        private readonly SqlService _sqlService;
        private readonly Session _session;

        /// <summary>
        /// Name of the command.
        /// </summary>
        public string Name => "showtable";

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowTableCommand"/> class.
        /// </summary>
        public ShowTableCommand(SqlService sqlService, Session session)
        {
            _sqlService = sqlService;
            _session = session;
        }

        /// <summary>
        /// Executes the logic to fetch and display a table's content, if the user has access.
        /// </summary>
        public void Execute(string[] args)
        {
            if (!_sqlService.IsConnected)
            {
                Shell.PrintError("You must be connected to a database.");
                return;
            }

            if (args.Length == 0)
            {
                Shell.PrintError("You must specify a table name.");
                return;
            }

            string tableName = args[0];

            if (_session.IsInRole("BOZO"))
            {
                if (!TableExists(tableName))
                {
                    Shell.PrintError($"Table [bold]{tableName}[/] does not exist.");
                    return;
                }
                _sqlService.ExecuteAndDisplay($"SELECT * FROM {tableName}");
                return;
            }

            if (!TableExistsRole(tableName))
            {
                Shell.PrintError($"Table [bold]{tableName}[/] does not exist or access is denied.");
                return;
            }

            _sqlService.ExecuteAndDisplay($"SELECT * FROM {tableName}");
        }

        /// <summary>
        /// Checks if a table exists in the current database (no role filtering).
        /// </summary>
        private bool TableExists(string tableName)
        {
            try
            {
                string query = $"SHOW TABLES LIKE '{tableName}'";
                using var cmd = new MySqlCommand(query, _sqlService.GetConnection());
                return cmd.ExecuteScalar() != null;
            }
            catch (Exception ex)
            {
                Shell.PrintError($"Error while checking table: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if a user with specific roles is allowed to access the table.
        /// </summary>
        private bool TableExistsRole(string tableName)
        {
            var userRoles = _session.CurrentUser?.Roles ?? new List<string>();

            var roleTables = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["CUISINIER"] = new List<string> { "plats", "evaluations" },
                ["CLIENT"] = new List<string> { "evaluations", "plats" },
                ["ADMIN"] = new List<string> {
                    "users", "roles", "user_roles", "plats", "commandes", "evaluations", "clients",
                    "cuisiniers", "stations_metro", "connexions_metro"
                }
            };

            var accessibleTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var role in userRoles)
            {
                if (roleTables.TryGetValue(role, out var tables))
                {
                    foreach (var table in tables) accessibleTables.Add(table);
                }
            }

            if (!accessibleTables.Contains(tableName))
            {
                Shell.PrintError($"Access to table '{tableName}' is denied.");
                return false;
            }

            try
            {
                string query = $"SHOW TABLES LIKE @tableName";
                using var cmd = new MySqlCommand(query, _sqlService.GetConnection());
                cmd.Parameters.AddWithValue("@tableName", tableName);
                return cmd.ExecuteScalar() != null;
            }
            catch (Exception ex)
            {
                Shell.PrintError($"SQL check error: {ex.Message}");
                return false;
            }
        }
    }
}
