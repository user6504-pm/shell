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
            // Vérifier si on est connecté
            if (!_sqlService.IsConnected) {
                AnsiConsole.MarkupLine("[red]⛔ Vous devez être connecté à une base de données.[/]");
                return;
            }

            if (_session.IsInRole("BOZO"))
                DisplayAllTables();
            else 
                DisplayRoleTables();

        }
            
        private void DisplayAllTables() {
            try {
                string query = $"SHOW TABLES"; // Requête pour récupérer toutes les tables
                using var cmd = new MySqlCommand(query, _sqlService.GetConnection());
                using var reader = cmd.ExecuteReader();

                if (!reader.HasRows) {
                    AnsiConsole.MarkupLine("[olive]⚠️ Aucune table trouvée dans la base de données.[/]");
                    return;
                }

                // Créer une table Spectre pour afficher les résultats
                var table = new Table().Border(TableBorder.Rounded).Expand();

                // Ajouter la colonne "Tables"
                table.AddColumn("[bold]Tables[/]");

                // Ajouter les lignes avec les noms des tables
                while (reader.Read()) {
                    table.AddRow(reader[0].ToString());
                }

                // Afficher la table avec les résultats
                AnsiConsole.Write(table);
            }
            catch (Exception ex) {
                AnsiConsole.MarkupLine($"[red]⛔ Erreur lors de la récupération des tables : {ex.Message}[/]");
            }
        }
        private void DisplayRoleTables() {
            var userRoles = _session.CurrentUser?.Roles ?? new List<string>();
            var visibleTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Définir les droits par rôle
            var roleTables = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                ["CUISINIER"] = new List<string> { "plats", "evaluations" },
                ["CLIENT"] = new List<string> { "evaluations", "plats" },
                ["ADMIN"] = new List<string> {
            "users", "roles", "user_roles", "plats", "commandes", "evaluations", "clients",
            "cuisiniers", "stations_metro", "connexions_metro"
        }
                // Ajoute d'autres rôles ici si nécessaire
            };

            // Collecter les tables autorisées pour tous les rôles de l'utilisateur
            foreach (var role in userRoles) {
                if (roleTables.TryGetValue(role, out var tables)) {
                    foreach (var table in tables)
                        visibleTables.Add(table);
                }
            }

            if (visibleTables.Count == 0) {
                AnsiConsole.MarkupLine("[yellow]⚠️ Aucun accès à des tables pour vos rôles actuels.[/]");
                return;
            }

            // Afficher les tables visibles
            var spectreTable = new Table().Border(TableBorder.Rounded).Expand();
            spectreTable.AddColumn("[bold]Tables accessibles[/]");

            foreach (var tableName in visibleTables.OrderBy(x => x)) {
                spectreTable.AddRow(tableName);
            }

            AnsiConsole.Write(spectreTable);
        }
    }
}
