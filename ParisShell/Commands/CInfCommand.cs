using Spectre.Console;
using ParisShell.Services;
using MySql.Data.MySqlClient;
using System;
using System.Text.RegularExpressions;
using MySqlX.XDevAPI;

namespace ParisShell.Commands {
    internal class CInfCommand : ICommand  // Renommé la classe en CinfCommand
    {
        private readonly SqlService _sqlService;
        private readonly Services.Session _session;

        public string Name => "cinf";  // Renommé la commande en "cinf"

        public CInfCommand(SqlService sqlService, Services.Session session) {
            _sqlService = sqlService;
            _session = session;
        }

        public void Execute(string[] args) {

            if (!_sqlService.IsConnected) {
                AnsiConsole.MarkupLine("[red]⛔ Vous n'êtes pas connecté à une base de données.[/]");
                return;
            }

            if (!_session.IsInRole("BOZO")) {
                AnsiConsole.MarkupLine("Permission denied, only bozo can");
                return;
            }

            // Afficher les informations de connexion
            DisplayConnectionInfo();
        }

        private void DisplayConnectionInfo() {
            try {
                // Récupérer les informations de la connexion (host, user, database, etc.)
                var connection = _sqlService.GetConnection();
                string host = connection.DataSource;
                string user = GetCurrentUser();
                string port = ExtractPortFromConnectionString(connection.ConnectionString);

                // Obtenir la base de données active
                string database = GetActiveDatabase();

                // Afficher les informations de connexion avec Spectre.Console
                AnsiConsole.MarkupLine("[bold]Informations de connexion :[/]");
                AnsiConsole.MarkupLine($"[bold]Base de données :[/] {database}");
                AnsiConsole.MarkupLine($"[bold]Utilisateur :[/] {user}");
                AnsiConsole.MarkupLine($"[bold]Hôte :[/] {host}");
                AnsiConsole.MarkupLine($"[bold]Port :[/] {port}");
            }
            catch (Exception ex) {
                AnsiConsole.MarkupLine($"[red]⛔ Erreur lors de la récupération des informations de connexion : {ex.Message}[/]");
            }
        }

        // Récupère l'utilisateur actuel de la connexion MySQL
        private string GetCurrentUser() {
            string user = "";

            try {
                // Utilise la requête SELECT USER() pour obtenir l'utilisateur actuel
                string query = "SELECT USER()";
                using var cmd = new MySqlCommand(query, _sqlService.GetConnection());
                var result = cmd.ExecuteScalar();

                if (result != null) {
                    user = result.ToString();
                }
            }
            catch (Exception ex) {
                AnsiConsole.MarkupLine($"[red]⛔ Erreur lors de la récupération de l'utilisateur : {ex.Message}[/]");
            }

            return user;
        }

        // Extrait le port à partir de la chaîne de connexion
        private string ExtractPortFromConnectionString(string connectionString) {
            // Expression régulière pour extraire le port
            var match = Regex.Match(connectionString, @"port=(\d+)");
            return match.Success ? match.Groups[1].Value : "Non spécifié";
        }

        // Récupère la base de données active via une requête SQL
        private string GetActiveDatabase() {
            string database = "";

            try {
                // Utilise la requête SELECT DATABASE() pour obtenir la base de données actuelle
                string query = "SELECT DATABASE()";
                using var cmd = new MySqlCommand(query, _sqlService.GetConnection());
                var result = cmd.ExecuteScalar();

                if (result != null) {
                    database = result.ToString();
                }
            }
            catch (Exception ex) {
                AnsiConsole.MarkupLine($"[red]⛔ Erreur lors de la récupération de la base de données active : {ex.Message}[/]");
            }

            return database;
        }
    }
}
