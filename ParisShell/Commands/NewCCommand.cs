using ParisShell.Services;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace ParisShell.Commands
{
    internal class NewCCommand : ICommand
    {
        public string Name => "newc";
        private readonly SqlService _sqlService;
        private readonly Session _session;

        public NewCCommand(SqlService sqlService, Session session)
        {
            _sqlService = sqlService;
            _session = session;
        }

        public void Execute(string[] args)
        {
            AnsiConsole.Clear();

            if (!_sqlService.IsConnected)
            {
                Shell.PrintError("You must be logged to order");
                return;
            }

            if (!_session.IsAuthenticated || !_session.IsInRole("CLIENT"))
            {
                Shell.PrintError("Only the clients can make an order");
                return;
            }
            List<(int Id, string Type, string Nationalite, decimal Prix)> platsDisponibles = new List<(int, string, string, decimal)>();

            MySqlCommand selectCmd = new MySqlCommand(@"
                SELECT p.plat_id, p.type_plat, p.nationalite, p.prix_par_personne
                FROM plats p
                WHERE p.plat_id NOT IN (SELECT plat_id FROM commandes);",
                _sqlService.GetConnection());

            MySqlDataReader reader = selectCmd.ExecuteReader();

            while (reader.Read())
            {
                int id = reader.GetInt32("plat_id");
                string type = reader.GetString("type_plat");
                string nat = reader.GetString("nationalite");
                decimal prix = reader.GetDecimal("prix_par_personne");

                platsDisponibles.Add((id, type, nat, prix));
            }

            reader.Close();
            selectCmd.Dispose();

            if (platsDisponibles.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]Aucun plat disponible pour le moment.[/]");
                return;
            }
            Table table = new Table().Border(TableBorder.Rounded);
            table.AddColumns("ID", "Type", "Nationality", "Price");

            foreach ((int id, string type, string nat, decimal prix) plat in platsDisponibles)
            {
                table.AddRow(
                    plat.id.ToString(),
                    plat.type,
                    plat.nat,
                    $"{plat.prix}"
                );
            }

            AnsiConsole.Write(table);
            int platIdChoisi = AnsiConsole.Ask<int>("Enter the [green]ID[/] of the dish to order:");

            (int id, string Type, string Nationalite, decimal Prix) platSelectionne = default;
            bool found = false;
            int i = 0;

            while (i < platsDisponibles.Count && !found)
            {
                (int Id, string Type, string Nationalite, decimal Prix) plat = platsDisponibles[i];

                if (plat.Id == platIdChoisi)
                {
                    platSelectionne = plat;
                    found = true;
                }

                i++;
            }
            if (!found)
            {
                Shell.PrintError("Dish not found.");
                return;
            }

            int quantite = AnsiConsole.Ask<int>("Desired quantity:");

            MySqlCommand insertCmd = new MySqlCommand(@"
                INSERT INTO commandes (client_id, plat_id, quantite)
                VALUES (@cid, @pid, @qte);",
                _sqlService.GetConnection());

            insertCmd.Parameters.AddWithValue("@cid", _session.CurrentUser.Id);
            insertCmd.Parameters.AddWithValue("@pid", platSelectionne.id);
            insertCmd.Parameters.AddWithValue("@qte", quantite);
            insertCmd.ExecuteNonQuery();
            insertCmd.Dispose();
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine($"[green]Order successfully recorded for dish ID {platSelectionne.id}![/]");
        }
    }
}
