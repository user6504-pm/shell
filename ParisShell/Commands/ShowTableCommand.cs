using Spectre.Console;
using ParisShell.Services;
using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace ParisShell.Commands {
    internal class ShowTableCommand : ICommand {
        private readonly SqlService _sqlService;
        private readonly Services.Session _session;

        public string Name => "showtable";

        public ShowTableCommand(SqlService sqlService, Session session) {
            _sqlService = sqlService;
            _session = session;
        }

        public void Execute(string[] args) {
            if (!_sqlService.IsConnected) {
                Shell.PrintError("You must be connected to a database.");
                return;
            }

            if (args.Length == 0) {
                Shell.PrintError("You must specify a table name.");
                return;
            }

            string tableName = args[0];

            if (_session.IsInRole("BOZO")) {
                if (!TableExists(tableName)) {
                    Shell.PrintError($"Table [bold]{tableName}[/] does not exist.");
                    return;
                }
                string _query = $"SELECT * FROM {tableName}";
                _sqlService.ExecuteAndDisplay(_query);
                return;
            }

            if (!TableExistsRole(tableName)) {
                Shell.PrintError($"Table [bold]{tableName}[/] does not exist or access is denied.");
                return;
            }

            string query = $"SELECT * FROM {tableName}";
            _sqlService.ExecuteAndDisplay(query);
        }

        private bool TableExists(string tableName) {
            try {
                string query = $"SHOW TABLES LIKE '{tableName}'";
                using var cmd = new MySqlCommand(query, _sqlService.GetConnection());
                var result = cmd.ExecuteScalar();
                return result != null;
            }
            catch (Exception ex) {
                Shell.PrintError($"Error while checking table: {ex.Message}");
                return false;
            }
        }

        private bool TableExistsRole(string tableName) {
            var userRoles = _session.CurrentUser?.Roles ?? new List<string>();
            var roleTables = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                ["CUISINIER"] = new List<string> { "plats", "evaluations" },
                ["CLIENT"] = new List<string> { "evaluations", "plats" },
                ["ADMIN"] = new List<string> {
                    "users", "roles", "user_roles", "plats", "commandes", "evaluations", "clients",
                    "cuisiniers", "stations_metro", "connexions_metro"
                }
            };

            var accessibleTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var role in userRoles) {
                if (roleTables.TryGetValue(role, out var tables)) {
                    foreach (var table in tables)
                        accessibleTables.Add(table);
                }
            }

            if (!accessibleTables.Contains(tableName)) {
                Shell.PrintError($"Access to table '{tableName}' is denied.");
                return false;
            }

            try {
                string query = $"SHOW TABLES LIKE @tableName";
                using var cmd = new MySqlCommand(query, _sqlService.GetConnection());
                cmd.Parameters.AddWithValue("@tableName", tableName);
                var result = cmd.ExecuteScalar();

                return result != null;
            }
            catch (Exception ex) {
                Shell.PrintError($"SQL check error: {ex.Message}");
                return false;
            }
        }
    }
}
