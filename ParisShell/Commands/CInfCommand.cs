using Spectre.Console;
using ParisShell.Services;
using MySql.Data.MySqlClient;
using System;
using System.Text.RegularExpressions;

namespace ParisShell.Commands {
    internal class CInfCommand : ICommand {
        private readonly SqlService _sqlService;
        private readonly Session _session;

        public string Name => "cinf";

        public CInfCommand(SqlService sqlService, Session session) {
            _sqlService = sqlService;
            _session = session;
        }

        public void Execute(string[] args) {
            if (!_sqlService.IsConnected) {
                Shell.PrintError("Not connected to any database.");
                return;
            }

            if (!_session.IsInRole("BOZO")) {
                Shell.PrintError("Permission denied. Only users with role 'BOZO' may access this information.");
                return;
            }

            DisplayConnectionInfo();
        }

        private void DisplayConnectionInfo() {
            try {
                var connection = _sqlService.GetConnection();
                string host = connection.DataSource;
                string user = GetCurrentUser();
                string port = ExtractPortFromConnectionString(connection.ConnectionString);
                string database = GetActiveDatabase();

                var panel = new Panel($"""
                    [white]Database[/]: {database}
                    [white]User[/]: {user}
                    [white]Host[/]: {host}
                    [white]Port[/]: {port}
                    """)
                    .Header("[bold deeppink4_2]Connection Info[/]")
                    .Border(BoxBorder.Rounded)
                    .BorderStyle(new Style(foreground: Color.SlateBlue1));

                AnsiConsole.Write(panel);
            }
            catch (Exception ex) {
                Shell.PrintError($"Failed to retrieve connection info: {ex.Message}");
            }
        }

        private string GetCurrentUser() {
            try {
                string query = "SELECT USER()";
                using var cmd = new MySqlCommand(query, _sqlService.GetConnection());
                var result = cmd.ExecuteScalar();
                return result?.ToString() ?? "unknown";
            }
            catch (Exception ex) {
                Shell.PrintError($"Failed to retrieve user: {ex.Message}");
                return "error";
            }
        }

        private string ExtractPortFromConnectionString(string connectionString) {
            var match = Regex.Match(connectionString, @"port=(\d+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : "unknown";
        }

        private string GetActiveDatabase() {
            try {
                string query = "SELECT DATABASE()";
                using var cmd = new MySqlCommand(query, _sqlService.GetConnection());
                var result = cmd.ExecuteScalar();
                return result?.ToString() ?? "unknown";
            }
            catch (Exception ex) {
                Shell.PrintError($"Failed to retrieve current database: {ex.Message}");
                return "error";
            }
        }
    }
}
