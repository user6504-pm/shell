using MySql.Data.MySqlClient;
using ParisShell.Services;
using Spectre.Console;
using System;
using System.Collections.Generic;

namespace ParisShell.Commands
{
    internal class AddQCommand : ICommand
    {
        public string Name => "addq";

        private readonly SqlService _sqlService;
        private readonly Session _session;

        public AddQCommand(SqlService sqlService, Session session)
        {
            _sqlService = sqlService;
            _session = session;
        }

        public void Execute(string[] args)
        {
            AnsiConsole.Clear();

            if (!_sqlService.IsConnected)
            {
                Shell.PrintError("You must be logged in to perform this command.");
                return;
            }

            if (!_session.IsAuthenticated || !_session.IsInRole("CUISINIER"))
            {
                Shell.PrintError("Only cooks can update dish quantities.");
                return;
            }

            List<(int Id, string Type, string Nationalite, decimal Prix, int Quantite)> plats = new();

            MySqlCommand selectCmd = new MySqlCommand(@"
                SELECT plat_id, type_plat, nationalite, prix_par_personne, quantite
                FROM plats
                WHERE user_id = @uid;",
                _sqlService.GetConnection());

            selectCmd.Parameters.AddWithValue("@uid", _session.CurrentUser.Id);

            using MySqlDataReader reader = selectCmd.ExecuteReader();

            while (reader.Read())
            {
                int id = reader.GetInt32("plat_id");
                string type = reader.GetString("type_plat");
                string nat = reader.GetString("nationalite");
                decimal prix = reader.GetDecimal("prix_par_personne");
                int quantite = reader.GetInt32("quantite");

                plats.Add((id, type, nat, prix, quantite));
            }

            selectCmd.Dispose();
            reader.Close();

            if (plats.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]You have no dishes to modify at the moment.[/]");
                return;
            }

            Table table = new Table().Border(TableBorder.Rounded);
            table.AddColumns("ID", "Type", "Nationality", "Price", "Quantity");

            foreach ((int id, string type, string nat, decimal prix, int quantite) plat in plats)
            {
                table.AddRow(
                    plat.id.ToString(),
                    plat.type,
                    plat.nat,
                    $"{plat.prix}",
                    plat.quantite.ToString()
                );
            }

            AnsiConsole.Write(table);
            (int Id, string Type, string Nationalite, decimal Prix, int Quantite) platSelectionne = default;
            bool found = false;

            while (!found)
            {
                int platIdChoisi = AnsiConsole.Ask<int>("Enter the [green]ID[/] of the dish to modify:");
                int i = 0;

                while (i < plats.Count && !found)
                {
                    (int Id, string Type, string Nationalite, decimal Prix, int Quantite) plat = plats[i];

                    if (plat.Id == platIdChoisi)
                    {
                        platSelectionne = plat;
                        found = true;
                    }

                    i++;
                }

                if (!found)
                {
                    Shell.PrintError("Dish not found. Please enter a valid ID.");
                }
            }




            int ajout = AnsiConsole.Ask<int>("Enter [green]quantity to add[/]:");

            MySqlCommand updateCmd = new MySqlCommand(@"
                UPDATE plats
                SET quantite = quantite + @ajout
                WHERE plat_id = @pid;",
                _sqlService.GetConnection());

            updateCmd.Parameters.AddWithValue("@ajout", ajout);
            updateCmd.Parameters.AddWithValue("@pid", platSelectionne.Id);

            int result = updateCmd.ExecuteNonQuery();
            updateCmd.Dispose();

            if (result > 0)
            {
                AnsiConsole.MarkupLine($"[green] Quantity successfully updated for dish ID {platSelectionne.Id}![/]");
            }
            else
            {
                Shell.PrintError(" An error occurred while updating the quantity.");
            }
        }
    }
}
