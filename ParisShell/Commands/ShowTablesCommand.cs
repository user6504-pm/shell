using Spectre.Console;
using ParisShell.Services;
using MySql.Data.MySqlClient;

namespace ParisShell.Commands {
    internal class ShowTablesCommand : ICommand {
        private readonly SqlService _sqlService;

        public string Name => "showtables";

        public ShowTablesCommand(SqlService sqlService) {
            _sqlService = sqlService;
        }

        public void Execute(string[] args) {
            // Vérifier si on est connecté
            if (!_sqlService.IsConnected) {
                AnsiConsole.MarkupLine("[red]⛔ Vous devez être connecté à une base de données.[/]");
                return;
            }

            // Récupérer toutes les tables
            DisplayAllTables();
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
    }
}
