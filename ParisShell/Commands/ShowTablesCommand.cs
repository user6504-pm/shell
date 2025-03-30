using Spectre.Console;
using ParisShell.Services;
using MySql.Data.MySqlClient;

namespace ParisShell.Commands {
    internal class ShowTablesCommand : ICommand {
        private readonly SqlService _sqlService;
        private readonly Services.Session _session;

        public string Name => "showtables";

        public ShowTablesCommand(SqlService sqlService, Session session) {
            _sqlService = sqlService;
            _session = session;
        }

        public void Execute(string[] args) {
            if (!_sqlService.IsConnected) {
                Shell.PrintError("You must be connected to a database.");
                return;
            }

            if (_session.IsInRole("BOZO"))
                _sqlService.ExecuteAndDisplay($"SHOW TABLES");
            else
                DisplayRoleTables();
        }

        private void DisplayRoleTables() {
            var userRoles = _session.CurrentUser?.Roles ?? new List<string>();
            var visibleTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var roleTables = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                ["CUISINIER"] = new List<string> { "plats", "evaluations" },
                ["CLIENT"] = new List<string> { "evaluations", "plats" },
                ["ADMIN"] = new List<string> {
                    "users", "roles", "user_roles", "plats", "commandes", "evaluations", "clients",
                    "cuisiniers", "stations_metro", "connexions_metro"
                }
            };

            foreach (var role in userRoles) {
                if (roleTables.TryGetValue(role, out var tables)) {
                    foreach (var table in tables)
                        visibleTables.Add(table);
                }
            }

            if (visibleTables.Count == 0) {
                Shell.PrintWarning("No tables accessible with your current roles.");
                return;
            }

            var spectreTable = new Table().Border(TableBorder.Rounded).Expand();
            spectreTable.AddColumn("[bold]Accessible Tables[/]");

            foreach (var tableName in visibleTables.OrderBy(x => x)) {
                spectreTable.AddRow(tableName);
            }

            AnsiConsole.Write(spectreTable);
        }
    }
}
