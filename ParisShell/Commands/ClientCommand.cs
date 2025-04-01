using Spectre.Console;
using MySql.Data.MySqlClient;
using ParisShell.Services;
using System.Drawing.Text;

namespace ParisShell.Commands
{
    internal class ClientCommand : ICommand
    {
        public string Name => "client";
        private readonly SqlService _sqlService;
        private readonly Session _session;

        public ClientCommand(SqlService sqlService, Session session)
        {
            _sqlService = sqlService;
            _session = session;
        }

        public void Execute(string[] args)
        {
            if (!_session.IsAuthenticated || !_session.IsInRole("CLIENT"))
            {
                Shell.PrintError("Access restricted to clients only.");
                return;
            }

            if (args.Length == 0)
            {
                Shell.PrintWarning("Usage: client [newc | orders | cancel]");
                return;
            }

            switch (args[0])
            {
                case "newc":
                    NewCommand();
                break;
                case "orders":
                    ShowMyOrders(); 
                break;
                case "cancel":
                    CancelMyOrder();
                break;
            }
        }
        private void NewCommand()
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
        private void ShowMyOrders()
        {
            MySqlCommand cmd = new MySqlCommand(@"
                SELECT c.commande_id, p.type_plat, p.nationalite,
               c.quantite, p.prix_par_personne,
               c.date_commande, c.statut
                FROM commandes c
                JOIN plats p ON c.plat_id = p.plat_id
                WHERE c.client_id = @cid
                ORDER BY c.date_commande DESC;",
                _sqlService.GetConnection());

            cmd.Parameters.AddWithValue("@cid", _session.CurrentUser.Id);

            using var reader = cmd.ExecuteReader();

            if (!reader.HasRows)
            {
                AnsiConsole.MarkupLine("[yellow]No orders found.[/]");
                return;
            }

            var table = new Table().Border(TableBorder.Rounded);
            table.AddColumns("ID Commande", "Type du plat", "Nationalité", "Quantité", "Prix", "Date", "Statut");

            while (reader.Read())
            {
                table.AddRow(
                    reader["commande_id"].ToString()!,
                    reader["type_plat"].ToString()!,
                    reader["nationalite"].ToString()!,
                    reader["quantite"].ToString()!,
                    $"{Convert.ToDecimal(reader["prix_par_personne"]):0.00}€",
                    Convert.ToDateTime(reader["date_commande"]).ToString("yyyy-MM-dd HH:mm"),
                    reader["statut"].ToString()!
                );
            }

            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[bold underline green]Vos commandes[/]");
            AnsiConsole.Write(table);
        }

        private void CancelMyOrder()
        {
            ShowMyOrders();
            int orderId = AnsiConsole.Ask<int>("Enter the [green]Order ID[/] to cancel:");

            string checkQuery = @"
            SELECT statut FROM commandes
            WHERE commande_id = @oid AND client_id = @cid";

            using var checkCmd = new MySqlCommand(checkQuery, _sqlService.GetConnection());
            checkCmd.Parameters.AddWithValue("@oid", orderId);
            checkCmd.Parameters.AddWithValue("@cid", _session.CurrentUser!.Id);

            var status = checkCmd.ExecuteScalar();

            if (status == null)
            {
                Shell.PrintError("Order not found or does not belong to you.");
                return;
            }

            if (status.ToString() != "EN_COURS")
            {
                Shell.PrintWarning("Only orders with status EN_COURS can be canceled.");
                return;
            }

            string cancelQuery = @"
            UPDATE commandes
            SET statut = 'ANNULEE'
            WHERE commande_id = @oid";

            using var updateCmd = new MySqlCommand(cancelQuery, _sqlService.GetConnection());
            updateCmd.Parameters.AddWithValue("@oid", orderId);

            int affected = updateCmd.ExecuteNonQuery();
            if (affected > 0)
                AnsiConsole.MarkupLine("[green]Order cancelled successfully.[/]");
            else
                Shell.PrintError("Failed to cancel the order.");
        }



    }
}
