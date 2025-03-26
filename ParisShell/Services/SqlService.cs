using System;
using System.Data;
using MySql.Data.MySqlClient;
using Spectre.Console;
using ParisShell.Models;
using Org.BouncyCastle.Bcpg;

namespace ParisShell.Services {
    internal class SqlService {
        private MySqlConnection? _connection;
        public MySqlConnection GetConnection() => _connection;

        public bool Connect(SqlConnectionConfig config) {
            if (!config.IsValid()) {
                AnsiConsole.MarkupLine("[maroon]⛔ Paramètres de connexion invalides.[/]");
                return false;
            }

            string connStr = $"SERVER={config.SERVER};PORT={config.PORT};" +
                             $"DATABASE={config.DATABASE};" +
                             $"UID={config.UID};PASSWORD={config.PASSWORD}";

            try {
                _connection = new MySqlConnection(connStr);
                _connection.Open();

                AnsiConsole.MarkupLine("[lime]✅ Connexion réussie à la base [bold]{0}[/][/]", config.DATABASE);
                return true;
            }
            catch (Exception ex) {
                AnsiConsole.MarkupLine("[maroon]Erreur de connexion :[/] " + ex.Message);
                return false;
            }
        }

        public void Disconnect() {
            if (_connection?.State == ConnectionState.Open) {
                _connection.Close();
                AnsiConsole.MarkupLine("[white]🔌 Déconnecté de la base de données.[/]");
            }
        }

        public bool IsConnected => _connection?.State == ConnectionState.Open;

        public void ExecuteAndDisplay(string sql) {
            if (!IsConnected) {
                AnsiConsole.MarkupLine("[maroon]⛔ Vous n'êtes pas connecté à une base de données.[/]");
                return;
            }

            try {
                using var cmd = new MySqlCommand(sql, _connection);
                using var reader = cmd.ExecuteReader();

                if (!reader.HasRows) {
                    AnsiConsole.MarkupLine("[olive]⚠️ Aucune donnée retournée.[/]");
                    return;
                }

                // Créer une table pour afficher les résultats
                var table = new Table().Border(TableBorder.Rounded);

                // Ajouter les colonnes (noms des colonnes SQL)
                for (int i = 0; i < reader.FieldCount; i++) {
                    table.AddColumn($"[bold]{reader.GetName(i)}[/]");
                }

                // Ajouter les lignes avec les résultats
                while (reader.Read()) {
                    var row = new List<string>();
                    for (int i = 0; i < reader.FieldCount; i++) {
                        row.Add(reader[i]?.ToString() ?? "");
                    }
                    table.AddRow(row.ToArray());
                }

                // Afficher la table
                AnsiConsole.Write(table);
            }
            catch (Exception ex) {
                AnsiConsole.MarkupLine($"[maroon]Erreur SQL :[/] {ex.Message}");
            }
        }
    }
}
