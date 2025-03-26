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
                AnsiConsole.MarkupLine("[red]⛔ Vous devez être connecté à une base de données.[/]");
                return;
            }

            if (args.Length == 0) {
                AnsiConsole.MarkupLine("[red]⛔ Vous devez spécifier un nom de table.[/]");
                return;
            }

            string tableName = args[0];




            if (_session.IsInRole("BOZO")) {
                // Vérifier si la table existe dans la base de données
                if (!TableExists(tableName)) {
                    AnsiConsole.MarkupLine($"[red]⛔ La table [bold]{tableName}[/] n'existe pas dans la base de données.[/]");
                    return;
                }
                string _query = $"SELECT * FROM {tableName}";
                _sqlService.ExecuteAndDisplay(_query);
                return;
            }
            if (!TableExistsRole(tableName)) {
                AnsiConsole.MarkupLine($"[red]⛔ La table [bold]{tableName}[/] n'existe pas dans la base de données.[/]");
                return;
            }
            string query = $"SELECT * FROM {tableName}";
            _sqlService.ExecuteAndDisplay(query);
            return;
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
        private bool TableExistsRole(string tableName) {
            // 🔒 Vérification d’autorisation par rôle
            var userRoles = _session.CurrentUser?.Roles ?? new List<string>();
            var roleTables = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                ["CUISINIER"] = new List<string> { "plats", "evaluations" },
                ["CLIENT"] = new List<string> { "evaluations", "plats" },
                ["ADMIN"] = new List<string> {
            "users", "roles", "user_roles", "plats", "commandes", "evaluations", "clients",
            "cuisiniers", "stations_metro", "connexions_metro"
        }
                // Ajoute d'autres rôles si nécessaire
            };

            // Tables accessibles à l’utilisateur
            var accessibleTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var role in userRoles) {
                if (roleTables.TryGetValue(role, out var tables)) {
                    foreach (var table in tables)
                        accessibleTables.Add(table);
                }
            }

            // 🔍 Est-ce que la table demandée est autorisée ?
            if (!accessibleTables.Contains(tableName)) {
                AnsiConsole.MarkupLine($"[red]⛔ Accès interdit à la table '{tableName}'.[/]");
                return false;
            }

            // ✅ Vérification SQL si la table existe vraiment
            try {
                string query = $"SHOW TABLES LIKE @tableName";
                using var cmd = new MySqlCommand(query, _sqlService.GetConnection());
                cmd.Parameters.AddWithValue("@tableName", tableName);
                var result = cmd.ExecuteScalar();

                return result != null;
            }
            catch (Exception ex) {
                AnsiConsole.MarkupLine($"[red]⛔ Erreur lors de la vérification SQL : {ex.Message}[/]");
                return false;
            }
        }
        
    }
    
}
