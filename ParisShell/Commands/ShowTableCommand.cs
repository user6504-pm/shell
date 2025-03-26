using Spectre.Console;
using ParisShell.Services;
using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace ParisShell.Commands {
    internal class ShowTableCommand : ICommand {
        private readonly SqlService _sqlService;

        public string Name => "showtable";

        public ShowTableCommand(SqlService sqlService) {
            _sqlService = sqlService;
        }

        public void Execute(string[] args) {
            if (args.Length == 0) {
                AnsiConsole.MarkupLine("[red]⛔ Vous devez spécifier un nom de table.[/]");
                return;
            }

            string tableName = args[0];

            if (!_sqlService.IsConnected) {
                AnsiConsole.MarkupLine("[red]⛔ Vous devez être connecté à une base de données.[/]");
                return;
            }

            // Vérifier si la table existe dans la base de données
            if (!TableExists(tableName)) {
                AnsiConsole.MarkupLine($"[red]⛔ La table [bold]{tableName}[/] n'existe pas dans la base de données.[/]");
                return;
            }

            // Utiliser la méthode ExecuteAndDisplay pour afficher les résultats de la table
            string query = $"SELECT * FROM {tableName}";
            _sqlService.ExecuteAndDisplay(query);
        }

        private bool TableExists(string tableName) {
            try {
                // Utiliser directement _sqlService pour exécuter la requête
                string query = $"SHOW TABLES LIKE '{tableName}'";
                using var cmd = new MySqlCommand(query, _sqlService.GetConnection());  // Correction ici
                var result = cmd.ExecuteScalar();

                return result != null; // Si la table existe, la requête retournera quelque chose
            }
            catch (Exception ex) {
                AnsiConsole.MarkupLine($"[red]⛔ Erreur lors de la vérification de la table : {ex.Message}[/]");
                return false;
            }
        }


    }
}
