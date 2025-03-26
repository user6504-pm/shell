﻿using Spectre.Console;
using MySql.Data.MySqlClient;
using ParisShell.Services;

namespace ParisShell.Commands {
    internal class CuisinierCommand : ICommand {
        public string Name => "cuisinier";
        private readonly SqlService _sqlService;
        private readonly Session _session;

        public CuisinierCommand(SqlService sqlService, Session session) {
            _sqlService = sqlService;
            _session = session;
        }

        public void Execute(string[] args) {
            if (!_session.IsAuthenticated || !_session.IsInRole("CUISINIER")) {
                Shell.PrintError("Access restricted to cuisiniers only.");
                return;
            }

            if (args.Length == 0) {
                Shell.PrintWarning("Usage: cuisinier [clients|stats|platdujour|ventes]");
                return;
            }

            switch (args[0]) {
                case "clients":
                    ShowClients();
                    break;
                case "stats":
                    ShowPlatsStats();
                    break;
                case "platdujour":
                    ShowPlatDuJour();
                    break;
                case "ventes":
                    ShowTotalVentesParPlat();
                    break;
                default:
                    Shell.PrintError("Unknown subcommand.");
                    break;
            }
        }

        private void ShowClients() {
            var filter = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("View served clients:")
                    .AddChoices("Since beginning", "By date range"));

            string dateCondition = "";

            if (filter == "By date range") {
                var from = AnsiConsole.Ask<string>("Start date (YYYY-MM-DD):");
                var to = AnsiConsole.Ask<string>("End date (YYYY-MM-DD):");
                dateCondition = $"AND c.date_commande BETWEEN '{from}' AND '{to}'";
            }

            string query = $@"
                SELECT DISTINCT u.nom, u.prenom, u.email
                FROM commandes c
                JOIN plats p ON c.plat_id = p.plat_id
                JOIN clients cl ON c.client_id = cl.client_id
                JOIN users u ON cl.client_id = u.user_id
                WHERE p.cuisinier_id = @_id {dateCondition}
                ORDER BY u.nom ASC";

            var table = new Table().Border(TableBorder.Rounded)
                .AddColumn("Last Name").AddColumn("First Name").AddColumn("Email");

            using var cmd = new MySqlCommand(query, _sqlService.GetConnection());
            cmd.Parameters.AddWithValue("@_id", _session.CurrentUser!.Id);
            using var reader = cmd.ExecuteReader();

            if (!reader.HasRows) {
                Shell.PrintWarning("No clients found.");
                return;
            }

            while (reader.Read()) {
                table.AddRow(reader["nom"].ToString(), reader["prenom"].ToString(), reader["email"].ToString());
            }

            AnsiConsole.Write(table);
        }

        private void ShowPlatsStats() {
            var table = new Table().Border(TableBorder.Rounded)
                .AddColumn("Dish Type")
                .AddColumn("Count");

            using var cmd = new MySqlCommand(@"
                SELECT type_plat, COUNT(*) AS nb
                FROM plats
                WHERE cuisinier_id = @_id
                GROUP BY type_plat
                ORDER BY nb DESC", _sqlService.GetConnection());

            cmd.Parameters.AddWithValue("@_id", _session.CurrentUser!.Id);
            using var reader = cmd.ExecuteReader();

            if (!reader.HasRows) {
                Shell.PrintWarning("No dishes found.");
                return;
            }

            while (reader.Read()) {
                table.AddRow(reader["type_plat"].ToString(), reader["nb"].ToString());
            }

            AnsiConsole.Write(table);
        }

        private void ShowPlatDuJour() {
            string query = @"
                SELECT plat_id, type_plat, nb_personnes, prix_par_personne
                FROM plats
                WHERE cuisinier_id = @_id AND date_fabrication = CURDATE()";

            var table = new Table().Border(TableBorder.Rounded)
                .AddColumn("ID").AddColumn("Type")
                .AddColumn("People").AddColumn("Price");

            using var cmd = new MySqlCommand(query, _sqlService.GetConnection());
            cmd.Parameters.AddWithValue("@_id", _session.CurrentUser!.Id);
            using var reader = cmd.ExecuteReader();

            if (!reader.HasRows) {
                Shell.PrintWarning("No dish of the day found.");
                return;
            }

            while (reader.Read()) {
                table.AddRow(
                    reader["plat_id"].ToString(),
                    reader["type_plat"].ToString(),
                    reader["nb_personnes"].ToString(),
                    reader["prix_par_personne"].ToString() + "€"
                );
            }

            AnsiConsole.Write(table);
        }

        private void ShowTotalVentesParPlat() {
            string query = @"
                SELECT p.plat_id, p.type_plat, COUNT(c.commande_id) AS commandes, 
                       SUM(c.quantite) AS total_qte,
                       SUM(c.quantite * p.prix_par_personne) AS total_vente
                FROM commandes c
                JOIN plats p ON p.plat_id = c.plat_id
                WHERE p.cuisinier_id = @_id
                GROUP BY p.plat_id, p.type_plat
                ORDER BY total_vente DESC";

            var table = new Table().Border(TableBorder.Rounded)
                .AddColumn("Dish ID")
                .AddColumn("Type")
                .AddColumn("Qty Sold")
                .AddColumn("Total Sales (€)");

            using var cmd = new MySqlCommand(query, _sqlService.GetConnection());
            cmd.Parameters.AddWithValue("@_id", _session.CurrentUser!.Id);
            using var reader = cmd.ExecuteReader();

            if (!reader.HasRows) {
                Shell.PrintWarning("No sales data found.");
                return;
            }

            while (reader.Read()) {
                table.AddRow(
                    reader["plat_id"].ToString(),
                    reader["type_plat"].ToString(),
                    reader["total_qte"].ToString(),
                    $"{reader["total_vente"]:0.00}€"
                );
            }

            AnsiConsole.Write(table);
        }
    }
}
