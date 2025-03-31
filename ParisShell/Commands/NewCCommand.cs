using ParisShell.Services;
using Spectre.Console;
using System;
using System.Collections.Generic;
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
                Shell.PrintError("You must be logged to order.");
                return;
            }

            if (!_session.IsAuthenticated || !_session.IsInRole("CLIENT"))
            {
                Shell.PrintError("Only clients can make an order.");
                return;
            }

            List<(int Id, string Type, string Nationalite, decimal Prix, int Quantite)> platsDisponibles = new List<(int, string, string, decimal, int)>();

            MySqlCommand selectCmd = new MySqlCommand(@"
                SELECT p.plat_id, p.type_plat, p.nationalite, p.prix_par_personne, p.quantite
                FROM plats p
                WHERE p.quantite > 0;",
                _sqlService.GetConnection());

            MySqlDataReader reader = selectCmd.ExecuteReader();

            while (reader.Read())
            {
                int id = reader.GetInt32("plat_id");
                string type = reader.GetString("type_plat");
                string nat = reader.GetString("nationalite");
                decimal prix = reader.GetDecimal("prix_par_personne");
                int quantite = reader.GetInt32("quantite");

                platsDisponibles.Add((id, type, nat, prix, quantite));
            }

            reader.Close();
            selectCmd.Dispose();

            if (platsDisponibles.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No available dishes for the moment.[/]");
                return;
            }

            Table table = new Table().Border(TableBorder.Rounded);
            table.AddColumns("ID", "Type", "Nationality", "Price", "Quantity");

            foreach ((int id, string type, string nat, decimal prix, int quantite) in platsDisponibles)
            {
                table.AddRow(
                    id.ToString(),
                    type,
                    nat,
                    $"{prix}",
                    quantite.ToString()
                );
            }

            AnsiConsole.Write(table);
            int platIdChoisi = AnsiConsole.Ask<int>("Enter the [green]ID[/] of the dish to order:");

            (int Id, string Type, string Nationalite, decimal Prix, int Quantite) platSelectionne = default;
            bool found = false;
            int i = 0;

            while (i < platsDisponibles.Count && !found)
            {
                (int Id, string Type, string Nationalite, decimal Prix, int Quantite) plat = platsDisponibles[i];

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

            int quantiteCommandee = 0;
            bool quantiteValide = false;

            while (!quantiteValide)
            {
                quantiteCommandee = AnsiConsole.Ask<int>("Enter the [green]quantity[/] to order:");

                if (quantiteCommandee <= 0)
                {
                    Shell.PrintError("Quantity must be greater than 0.");
                }
                else if (quantiteCommandee > platSelectionne.Quantite)
                {
                    Shell.PrintError("Not enough quantity available for this dish.");
                }
                else
                {
                    quantiteValide = true;
                }
            }

            MySqlCommand insertCmd = new MySqlCommand(@"
                INSERT INTO commandes (client_id, plat_id, quantite)
                VALUES (@cid, @pid, @qte);",
                _sqlService.GetConnection());

            insertCmd.Parameters.AddWithValue("@cid", _session.CurrentUser.Id);
            insertCmd.Parameters.AddWithValue("@pid", platSelectionne.Id);
            insertCmd.Parameters.AddWithValue("@qte", quantiteCommandee);
            insertCmd.ExecuteNonQuery();
            insertCmd.Dispose();

            int nouvelleQuantite = platSelectionne.Quantite - quantiteCommandee;

            MySqlCommand updateCmd = new MySqlCommand(@"
                UPDATE plats
                SET quantite = @newQty
                WHERE plat_id = @pid;",
                _sqlService.GetConnection());

            updateCmd.Parameters.AddWithValue("@newQty", nouvelleQuantite);
            updateCmd.Parameters.AddWithValue("@pid", platSelectionne.Id);
            updateCmd.ExecuteNonQuery();
            updateCmd.Dispose();

            AnsiConsole.Clear();
            AnsiConsole.MarkupLine($"[green] Order recorded for dish ID {platSelectionne.Id} ({quantiteCommandee} unit(s)).[/]");
        }
    }
}
