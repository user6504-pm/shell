using Spectre.Console;
using ParisShell.Services;
using MySql.Data.MySqlClient;

namespace ParisShell.Commands
{

    /// <summary>
    /// Command to list all tables accessible to the current user's roles.
    /// </summary>
    internal class ShowTablesCommand : ICommand
    {
        private readonly SqlService _sqlService;
        private readonly Session _session;

        /// <summary>
        /// Command name.
        /// </summary>
        public string Name => "showtables";

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowTablesCommand"/> class.
        /// </summary>
        public ShowTablesCommand(SqlService sqlService, Session session)
        {
            _sqlService = sqlService;
            _session = session;
        }

        /// <summary>
        /// Executes the logic to display table names accessible to the current user.
        /// </summary>
        public void Execute(string[] args)
        {
            if (!_sqlService.IsConnected)
            {
                Shell.PrintError("You must be connected to a database.");
                return;
            }

            if (!_session.IsAuthenticated)
            {
                Shell.PrintError("You must be logged in to view accessible tables.");
                return;
            }

            if (_session.IsInRole("BOZO"))
            {
                _sqlService.ExecuteAndDisplay(@"
                    SELECT table_name AS 'Table' 
                    FROM information_schema.tables 
                    WHERE table_schema = 'Livininparis_219'
                ");
            }
            else
            {
                DisplayRoleTables();
            }
        }

        /// <summary>
        /// Displays the list of tables accessible to the user's current roles.
        /// </summary>
        private void DisplayRoleTables()
        {
            var userRoles = _session.CurrentUser?.Roles ?? new List<string>();
            var visibleTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var roleTables = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["CUISINIER"] = new List<string> { "plats", "evaluations" },
                ["CLIENT"] = new List<string> { "evaluations", "plats" },
                ["ADMIN"] = new List<string> {
                    "users", "roles", "user_roles", "plats", "commandes", "evaluations", "clients",
                    "cuisiniers", "stations_metro", "connexions_metro"
                }
            };

            foreach (var role in userRoles)
            {
                if (roleTables.TryGetValue(role, out var tables))
                {
                    foreach (var table in tables)
                        visibleTables.Add(table);
                }
            }

            if (visibleTables.Count == 0)
            {
                Shell.PrintWarning("No tables accessible with your current roles.");
                return;
            }

            var spectreTable = new Table().Border(TableBorder.Rounded).Expand();
            spectreTable.AddColumn("[bold]Accessible Tables[/]");

            foreach (var tableName in visibleTables.OrderBy(x => x))
            {
                spectreTable.AddRow(tableName);
            }

            AnsiConsole.Write(spectreTable);
        }
    }
}
