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

            DisplayConnectionInfo();
        }

        private void DisplayConnectionInfo()
        {
            try
            {
                var connection = _sqlService.GetConnection();
                string host = connection.DataSource;
                string user = GetCurrentUser();
                string port = ExtractPort(connection.ConnectionString);
                string database = GetActiveDatabase();
                string machine = Environment.MachineName;
                string os = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
                string dotnet = Environment.Version.ToString();
                string userRole = string.Join(", ", _session.CurrentUser?.Roles ?? new List<string>());

                // Your rocket ASCII Art
                string asciiArt = """
[bold red]         __
        / /\
       / /  \
      / /    \__________
     / /      \        /\
    /_/        \      / /
 ___\ \      ___\____/_/_
/____\ \    /___________/\
\     \ \   \           \ \
 \     \ \   \____       \ \
  \     \ \  /   /\       \ \
   \   / \_\/   / /        \ \
    \ /        / /__________\/
     /        / /     /
    /        / /     /
   /________/ /\    /
   \________\/\ \  /
               \_\/[/]
""";

                // Info Table
                var infoTable = new Table()
                    .Border(TableBorder.Rounded)
                    .AddColumn("[bold deeppink4_2]Information[/]")
                    .AddColumn("[bold]Value[/]");

                infoTable.AddRow("[bold deeppink4_2]Host[/]", host);
                infoTable.AddRow("[bold deeppink4_2]Port[/]", port);
                infoTable.AddRow("[bold deeppink4_2]Database[/]", database);
                infoTable.AddRow("[bold deeppink4_2]User[/]", user);
                infoTable.AddRow("[bold deeppink4_2]Role(s)[/]", userRole);
                infoTable.AddRow("[bold deeppink4_2]Machine[/]", machine);
                infoTable.AddRow("[bold deeppink4_2]OS[/]", os);
                infoTable.AddRow("[bold deeppink4_2].NET[/]", dotnet);

                var layout = new Grid();
                layout.AddColumn();
                layout.AddColumn();
                layout.AddRow(new Markup(asciiArt), infoTable);

                AnsiConsole.Write(new Panel(layout)
                    .Header("[bold deeppink4_2]Session & Connection Info[/]")
                    .Border(BoxBorder.Double)
                    .BorderStyle(new Style(foreground: Color.SlateBlue1))
                    .Padding(1, 1));
            }
            catch (Exception ex)
            {
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

        private string ExtractPort(string connectionString) {
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
