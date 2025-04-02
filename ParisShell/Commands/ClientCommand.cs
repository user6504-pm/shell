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

            List<(int Id, string Name, string Type, string Nationalite, decimal Prix, int Quantite)> platsDisponibles = new List<(int, string, string, string, decimal, int)>();

            MySqlCommand selectCmd = new MySqlCommand(@"
                SELECT p.plat_id, p.plat_name, p.type_plat, p.nationalite, p.prix_par_personne, p.quantite
                FROM plats p
                WHERE p.quantite > 0;",
                _sqlService.GetConnection());

            MySqlDataReader reader = selectCmd.ExecuteReader();

            while (reader.Read())
            {
                int id = reader.GetInt32("plat_id");
                string name = reader.GetString("plat_name");
                string type = reader.GetString("type_plat");
                string nat = reader.GetString("nationalite");
                decimal prix = reader.GetDecimal("prix_par_personne");
                int quantite = reader.GetInt32("quantite");

                platsDisponibles.Add((id, name, type, nat, prix, quantite));
            }

            reader.Close();
            selectCmd.Dispose();

            if (platsDisponibles.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No available dishes for the moment.[/]");
                return;
            }

            Table table = new Table().Border(TableBorder.Rounded);
            table.AddColumns("ID", "Name", "Type", "Nationality", "Price", "Quantity");

            foreach ((int id, string name, string type, string nat, decimal prix, int quantite) in platsDisponibles)
            {
                table.AddRow(
                    id.ToString(),
                    name,
                    type,
                    nat,
                    prix.ToString(),
                    quantite.ToString()
                );
            }

            AnsiConsole.Write(table);
            bool confirm = true;
            int platIdChoisi = AnsiConsole.Ask<int>("Enter the [green]ID[/] of the dish to order: (type 0 to cancel)");

            if (platIdChoisi == 0)
            {
                confirm = false;
            }
            if (confirm)
            {
                (int Id, string Name, string Type, string Nationalite, decimal Prix, int Quantite) platSelectionne = default;
                bool found = false;
                int i = 0;

                while (i < platsDisponibles.Count && !found)
                {
                    (int Id, string Name, string Type, string Nationalite, decimal Prix, int Quantite) plat = platsDisponibles[i];

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
                string confirmation = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Are you sure you want to order?")
                        .AddChoices("Yes", "No")
                );
                bool conf = true;
                if (confirmation == "No")
                {
                    AnsiConsole.MarkupLine("[yellow]Command aborted by the user.[/]");
                    conf = false;
                }
                if (conf)
                {
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
                    AnsiConsole.MarkupLine($"[green] Order recorded for dish '{platSelectionne.Name}' (ID {platSelectionne.Id}, {quantiteCommandee} unit(s)).[/]");
                }
            }
        }



        private void ShowMyOrders()
        {
            MySqlCommand cmd = new MySqlCommand(@"
                SELECT c.commande_id, p.plat_name, p.type_plat, p.nationalite,
                       c.quantite, p.prix_par_personne,
                       c.date_commande, c.statut
                FROM commandes c
                JOIN plats p ON c.plat_id = p.plat_id
                WHERE c.client_id = @cid
                ORDER BY c.date_commande DESC;",
                _sqlService.GetConnection());

            cmd.Parameters.AddWithValue("@cid", _session.CurrentUser.Id);

            MySqlDataReader reader = cmd.ExecuteReader();

            bool hasOrders = reader.HasRows;

            if (!hasOrders)
            {
                AnsiConsole.MarkupLine("[yellow]No orders found.[/]");
            }

            if (hasOrders)
            {
                Table table = new Table().Border(TableBorder.Rounded);
                table.AddColumns("ID Commande", "Nom du plat", "Type", "Nationalité", "Quantité", "Prix", "Date", "Statut");

                while (reader.Read())
                {
                    table.AddRow(
                        reader["commande_id"].ToString(),
                        reader["plat_name"].ToString(),
                        reader["type_plat"].ToString(),
                        reader["nationalite"].ToString(),
                        reader["quantite"].ToString(),
                        string.Format("{0:0.00}", Convert.ToDecimal(reader["prix_par_personne"])),
                        Convert.ToDateTime(reader["date_commande"]).ToString("yyyy-MM-dd HH:mm"),
                        reader["statut"].ToString()
                    );
                }

                AnsiConsole.Clear();
                AnsiConsole.MarkupLine("[bold underline green]Vos commandes[/]");
                AnsiConsole.Write(table);
            }

            reader.Close();
            cmd.Dispose();
        }


        private void CancelMyOrder()
        {
            ShowMyOrders();

            int orderId = -1;
            string statut = null;
            int platId = -1;
            int quantite = 0;

            while (statut == null)
            {
                int saisie = AnsiConsole.Ask<int>("Enter the [green]Order ID[/] to cancel:");

                string checkQuery = @"
                SELECT statut, plat_id, quantite
                FROM commandes 
                WHERE commande_id = @oid AND client_id = @cid";

                MySqlCommand checkCmd = new MySqlCommand(checkQuery, _sqlService.GetConnection());
                checkCmd.Parameters.AddWithValue("@oid", saisie);
                checkCmd.Parameters.AddWithValue("@cid", _session.CurrentUser.Id);

                MySqlDataReader reader = checkCmd.ExecuteReader();
                if (reader.Read())
                {
                    statut = reader.GetString("statut");
                    platId = reader.GetInt32("plat_id");
                    quantite = reader.GetInt32("quantite");
                    orderId = saisie;
                }
                else
                {
                    Shell.PrintError("Order not found or does not belong to you. Try again.");
                }

                reader.Close();
                checkCmd.Dispose();
            }

            bool confirm = true;
            if (statut != "EN_COURS")
            {
                Shell.PrintWarning("Only orders with status EN_COURS can be canceled.");
                confirm = false;
            }
            if (confirm)
            {
                string confirmation = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Are you sure you want to cancel this order?")
                        .AddChoices("Yes", "No")
                );

                if (confirmation == "No")
                {
                    AnsiConsole.MarkupLine("[yellow]Cancellation aborted by the user.[/]");
                    confirm = false;
                }
                if (confirm)
                {
                    string updatePlatQuery = @"
                    UPDATE plats 
                    SET quantite = quantite + @qte 
                    WHERE plat_id = @pid";

                    MySqlCommand updatePlatCmd = new MySqlCommand(updatePlatQuery, _sqlService.GetConnection());
                    updatePlatCmd.Parameters.AddWithValue("@qte", quantite);
                    updatePlatCmd.Parameters.AddWithValue("@pid", platId);
                    updatePlatCmd.ExecuteNonQuery();
                    updatePlatCmd.Dispose();

                    string cancelQuery = @"
                    UPDATE commandes 
                    SET statut = 'ANNULEE' 
                    WHERE commande_id = @oid";

                    MySqlCommand cancelCmd = new MySqlCommand(cancelQuery, _sqlService.GetConnection());
                    cancelCmd.Parameters.AddWithValue("@oid", orderId);
                    cancelCmd.ExecuteNonQuery();
                    cancelCmd.Dispose();

                    AnsiConsole.Clear();
                    AnsiConsole.MarkupLine("[yellow]Cancellation done.[/]");
                }


            }


        }

    }
}
    

