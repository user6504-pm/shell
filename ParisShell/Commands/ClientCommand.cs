using MySql.Data.MySqlClient;
using Mysqlx.Crud;
using ParisShell.Graph;
using ParisShell.Models;
using ParisShell.Services;
using SkiaSharp;
using Spectre.Console;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace ParisShell.Commands
{

    /// <summary>
    /// Command available only for users with the CLIENT role.
    /// Allows them to place orders, view existing ones, cancel orders, or check delivery routes.
    /// </summary>
    internal class ClientCommand : ICommand
    {
        /// <summary>
        /// The name used to invoke this command from the shell.
        /// </summary>
        public string Name => "client";

        private readonly SqlService _sqlService;
        private readonly Session _session;

        /// <summary>
        /// Constructor that initializes the ClientCommand with necessary services.
        /// </summary>
        /// <param name="sqlService">Service used to access the MySQL database.</param>
        /// <param name="session">Session containing authentication and user info.</param>
        public ClientCommand(SqlService sqlService, Session session)
        {
            _sqlService = sqlService;
            _session = session;
        }

        /// <summary>
        /// Main command handler. Parses subcommands and redirects to the appropriate method.
        /// </summary>
        /// <param name="args">Command-line arguments passed after 'client'.</param>
        public void Execute(string[] args)
        {
            // Check if the user is logged in and has the CLIENT role
            if (!_session.IsAuthenticated || !_session.IsInRole("CLIENT"))
            {
                Shell.PrintError("Access restricted to clients only.");
                return;
            }

            if (args.Length == 0)
            {
                Shell.PrintWarning("Usage: client neworder | orders | cancel | order-travel (id)");
                return;
            }

            switch (args[0])
            {
                case "neworder":
                    NewCommand();
                    break;
                case "orders":
                    ShowMyOrders();
                    break;
                case "cancel":
                    CancelMyOrder();
                    break;
                case "order-travel":
                    OrderTravel(args);
                    break;
                case "evaluate":
                    EvaluateOrder();
                    break;
                default:
                    Shell.PrintError("Unknown subcommand.");
                    break;


            }
        }

        /// <summary>
        /// Displays the SVG route between the cook and the client for a specific order.
        /// </summary>
        /// <param name="args">Arguments provided by the user. Must contain the order ID.</param>
        private void OrderTravel(string[] args)
        {
            if (args.Length < 2 || !int.TryParse(args[1], out int commandeId))
            {
                Shell.PrintError("Usage: client order-travel {commande_id}");
                return;
            }

            if (!_sqlService.IsConnected || !_session.IsAuthenticated)
            {
                Shell.PrintError("You must be connected to view travel path.");
                return;
            }

            int clientId = _session.CurrentUser.Id;
            string query = @"SELECT u.metroproche AS client_station, cu.metroproche AS cook_station
                     FROM commandes c
                     JOIN plats p ON c.plat_id = p.plat_id
                     JOIN users cu ON cu.user_id = p.user_id
                     JOIN users u ON u.user_id = c.client_id
                     WHERE c.commande_id = @id AND c.client_id = @clientId";

            using var cmd = new MySqlCommand(query, _sqlService.GetConnection());
            cmd.Parameters.AddWithValue("@id", commandeId);
            cmd.Parameters.AddWithValue("@clientId", clientId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                Shell.PrintError("Commande introuvable ou non liée à votre compte.");
                return;
            }

            int cookStationId = reader.GetInt32("cook_station");
            int clientStationId = reader.GetInt32("client_station");
            reader.Close();

            Graph<StationData> graphe = GraphLoader.Construire(_sqlService.GetConnection());
            var noeudsDict = graphe.ObtenirNoeuds().ToDictionary(n => n.Id);

            if (!noeudsDict.TryGetValue(cookStationId, out var noeudDepart) ||
                !noeudsDict.TryGetValue(clientStationId, out var noeudArrivee))
            {
                Shell.PrintError("Impossible de trouver les stations associées.");
                return;
            }

            var chemin = graphe.BellmanFordCheminPlusCourt(noeudDepart, noeudArrivee);

            if (chemin == null || chemin.Count == 0)
            {
                Shell.PrintError("Aucun chemin trouvé entre le cuisinier et le client.");
                return;
            }

            string nameFile = $"commande_{commandeId}_trajet.svg";
            graphe.ExporterSvg(nameFile, chemin);
            if (File.Exists(nameFile))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = nameFile,
                    UseShellExecute = true
                });
            }
            else
            {
                Console.WriteLine("SVG file not found.");
            }
        }



        /// <summary>
        /// Retrieves the metro station ID associated with a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>The station_id from the 'stations_metro' table.</returns>
        private int GetStationIdFromUser(int userId)
        {
            const string query = @"
        SELECT s.station_id
        FROM users u
        JOIN stations_metro s ON u.metroproche = s.station_id
        WHERE u.user_id = @id;
    ";

            using var cmd = new MySqlCommand(query, _sqlService.GetConnection());
            cmd.Parameters.AddWithValue("@id", userId);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }


        /// <summary>
        /// Allows the client to create a new order from the list of available dishes.
        /// </summary>
        private void NewCommand()
        {
            var ratingsDict = GetAverageRatings();
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

            List<(int PlatId, string Name, string Type, string Nat, decimal Prix, int Quantite, int UserId)> platsTemp = new();

            MySqlCommand selectCmd = new MySqlCommand(@"
            SELECT p.plat_id, p.plat_name, p.type_plat, p.nationalite, p.prix_par_personne, p.quantite, p.user_id
            FROM plats p
            WHERE p.quantite > 0
            ORDER BY p.prix_par_personne;",
                _sqlService.GetConnection());

            using (MySqlDataReader reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    platsTemp.Add((
                        reader.GetInt32("plat_id"),
                        reader.GetString("plat_name"),
                        reader.GetString("type_plat"),
                        reader.GetString("nationalite"),
                        reader.GetDecimal("prix_par_personne"),
                        reader.GetInt32("quantite"),
                        reader.GetInt32("user_id")
                    ));
                }
            }

            selectCmd.Dispose();

            Graph<StationData> graph = GraphLoader.Construire(_sqlService.GetConnection());
            var nodesDict = graph.ObtenirNoeuds().ToDictionary(n => n.Id);
            List<(int Id, string Name, string Type, string Nationalite, decimal Prix, int Quantite, decimal Temps)> DisponibleDishes = new();

            AnsiConsole.Status()
            .Start("Loading dishes...", ctx =>
            {
                try
                {
                    ctx.Spinner(Spinner.Known.Flip);
                    ctx.SpinnerStyle(Style.Parse("green"));
                    foreach (var plat in platsTemp)
                    {
                        int user_station = GetStationIdFromUser(plat.UserId);
                        int session_station = GetStationIdFromUser(_session.CurrentUser.Id);

                        if (!nodesDict.TryGetValue(user_station, out var noeudDepart)) continue;
                        if (!nodesDict.TryGetValue(session_station, out var noeudArrivee)) continue;

                        var chemin = graph.DijkstraCheminPlusCourt(noeudDepart, noeudArrivee);
                        decimal temps = graph.TempsCheminStations(chemin);

                        DisponibleDishes.Add((plat.PlatId, plat.Name, plat.Type, plat.Nat, plat.Prix, plat.Quantite, temps));
                    }
                }
                catch (Exception ex)
                {
                    Shell.PrintError($"Import error: {ex.Message}");
                }
            });

            if (DisponibleDishes.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No available dishes for the moment.[/]");
                return;
            }

            Table table = new Table().Border(TableBorder.Rounded);
            table.AddColumns("ID", "Name", "Type", "Nationality", "Price", "Quantity", "Time", "Notation");

            foreach (var d in DisponibleDishes)
            {
                string note = ratingsDict.ContainsKey(d.Id) ? ratingsDict[d.Id] : "Pas encore noté";

                table.AddRow(
                    d.Id.ToString(),
                    d.Name,
                    d.Type,
                    d.Nationalite,
                    d.Prix.ToString(),
                    d.Quantite.ToString(),
                    d.Temps.ToString("0") + " min",
                    note
                );
            }


            AnsiConsole.Write(table);

            int IdDishSelected = AnsiConsole.Ask<int>("Enter the [green]ID[/] of the dish to order: (type 0 to cancel)");
            if (IdDishSelected == 0) 
            {
                AnsiConsole.MarkupLine("[yellow]Order cancelled.[/]"); return;
            }

            var platSelectionne = DisponibleDishes.FirstOrDefault(p => p.Id == IdDishSelected);
            if (platSelectionne == default)
            {
                Shell.PrintError("Dish not found.");
                return;
            }

            int quantityOrdered = 0;
            bool quantityAccepted = false;

            while (!quantityAccepted)
            {
                quantityOrdered = AnsiConsole.Ask<int>("Enter the [green]quantity[/] to order:");
                if (quantityOrdered <= 0)
                    Shell.PrintError("Quantity must be greater than 0.");
                else if (quantityOrdered > platSelectionne.Quantite)
                    Shell.PrintError("Not enough quantity available for this dish.");
                else
                    quantityAccepted = true;
            }

            string confirmation = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Are you sure you want to order?")
                    .AddChoices("Yes", "No")
            );
            if (confirmation == "No")
            {
                AnsiConsole.MarkupLine("[yellow]Command aborted by the user.[/]");
                return;
            }

            MySqlCommand insertCmd = new MySqlCommand(@"
            INSERT INTO commandes (client_id, plat_id, quantite)
            VALUES (@cid, @pid, @qte);",
                _sqlService.GetConnection());

            insertCmd.Parameters.AddWithValue("@cid", _session.CurrentUser.Id);
            insertCmd.Parameters.AddWithValue("@pid", platSelectionne.Id);
            insertCmd.Parameters.AddWithValue("@qte", quantityOrdered);
            insertCmd.ExecuteNonQuery();
            insertCmd.Dispose();

            AnsiConsole.Clear();
            AnsiConsole.MarkupLine($"[green] Order recorded for dish '{platSelectionne.Name}' (ID {platSelectionne.Id}, {quantityOrdered} unit(s)).[/]");
        }




        /// <summary>
        /// Displays all the orders made by the currently logged-in client.
        /// Orders are shown in a table format, sorted by date in descending order.
        /// </summary>
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
                table.AddColumns("ID Commande", "Nom du plat", "Type", "Nationalité", "Quantité", "Prix total", "Date", "Statut");

                while (reader.Read())
                {
                    table.AddRow(
                        reader["commande_id"].ToString(),
                        reader["plat_name"].ToString(),
                        reader["type_plat"].ToString(),
                        reader["nationalite"].ToString(),
                        reader["quantite"].ToString(),
                        string.Format("{0:0.00}", Convert.ToDecimal(reader["prix_par_personne"]) * Convert.ToDecimal(reader["quantite"])),
                        Convert.ToDateTime(reader["date_commande"]).ToString("yyyy-MM-dd HH:mm"),
                        reader["statut"].ToString()
                    );
                }

                AnsiConsole.MarkupLine("[bold underline green]Vos commandes[/]");
                AnsiConsole.Write(table);
            }

            reader.Close();
            cmd.Dispose();
        }



        /// <summary>
        /// Allows the client to cancel one of their orders, only if it's still in progress ("EN_COURS").
        /// The dish quantity is restored in the inventory and order status is updated.
        /// </summary>
        private void CancelMyOrder()
        {
            ShowMyOrders();
            int userId = _session.CurrentUser.Id;

            MySqlCommand verify = new MySqlCommand(@"
                SELECT commande_id 
                FROM commandes 
                WHERE client_id = @cid",
            _sqlService.GetConnection());

            verify.Parameters.AddWithValue("@cid", userId);

            MySqlDataReader verifyReader = verify.ExecuteReader();

            if (!verifyReader.Read())
            {
                verifyReader.Close();
                verify.Dispose();
                return;
            }

            verifyReader.Close();
            verify.Dispose();
            int orderId = -1;
            string status = null;
            int IdDish = -1;
            int quantity = 0;
            bool found = false;

            while (!found)
            {
                int entry = AnsiConsole.Ask<int>("Enter the [green]Order ID[/] to cancel (0 to cancel) :");
                if (entry == 0)
                {
                    AnsiConsole.MarkupLine("Cancellation aborted by the user");
                    return;
                }

                string checkQuery = @"
                SELECT statut, plat_id, quantite
                FROM commandes 
                WHERE commande_id = @oid AND client_id = @cid";

                MySqlCommand checkCmd = new MySqlCommand(checkQuery, _sqlService.GetConnection());
                checkCmd.Parameters.AddWithValue("@oid", entry);
                checkCmd.Parameters.AddWithValue("@cid", _session.CurrentUser.Id);

                MySqlDataReader reader = checkCmd.ExecuteReader();
                if (reader.Read())
                {
                    status = reader.GetString("statut");
                    IdDish = reader.GetInt32("plat_id");
                    quantity = reader.GetInt32("quantite");
                    orderId = entry;
                    if (status != "EN_ATTENTE")
                    {
                        AnsiConsole.MarkupLine("Only orders with status [green]EN_ATTENTE[/] can be canceled. Try again");
                    }
                    else found = true;
                }
                else if (entry != 0 && !found)
                {
                    AnsiConsole.MarkupLine("[red]Order not found or does not belong to you[/]. Try again.");
                }




                reader.Close();
                checkCmd.Dispose();
            }



            string confirmation = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Are you sure you want to cancel this order?")
                    .AddChoices("Yes", "No")
            );

            if (confirmation == "No")
            {
                AnsiConsole.MarkupLine("[yellow]Cancellation aborted by the user.[/]");
                return;
            }
            
            MySqlCommand statusCmd = new MySqlCommand(@"
                UPDATE commandes
                SET statut = 'ANNULEE'
                WHERE commande_id = @cid",
                _sqlService.GetConnection());

            statusCmd.Parameters.AddWithValue("@cid", IdDish);
            statusCmd.ExecuteNonQuery();
            statusCmd.Dispose();

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
        private void EvaluateOrder()
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[green]Evaluate a delivered order:[/]");

            MySqlCommand cmd = new MySqlCommand(@"
                SELECT c.commande_id, p.plat_name, p.type_plat, p.nationalite,
                c.quantite, p.prix_par_personne,
                c.date_commande, c.statut
                FROM commandes c
                JOIN plats p ON c.plat_id = p.plat_id
                WHERE c.client_id = @cid
                AND c.statut = 'LIVREE'
                AND c.commande_id NOT IN (SELECT commande_id FROM evaluations)
                ORDER BY c.date_commande DESC;",
                _sqlService.GetConnection());

            cmd.Parameters.AddWithValue("@cid", _session.CurrentUser.Id);
            MySqlDataReader reader = cmd.ExecuteReader();

            List<int> orderIds = new();
            Table table = new Table().Border(TableBorder.Rounded);
            table.AddColumns("ID Commande", "Nom du plat", "Type", "Nationalité", "Quantité", "Prix total", "Date", "Statut");

            while (reader.Read())
            {
                int id = reader.GetInt32("commande_id");
                orderIds.Add(id);

                table.AddRow(
                    id.ToString(),
                    reader["plat_name"].ToString(),
                    reader["type_plat"].ToString(),
                    reader["nationalite"].ToString(),
                    reader["quantite"].ToString(),
                    string.Format("{0:0.00}",
                    Convert.ToDecimal(reader["prix_par_personne"]) * Convert.ToDecimal(reader["quantite"])),
                    Convert.ToDateTime(reader["date_commande"]).ToString("yyyy-MM-dd HH:mm"),
                    reader["statut"].ToString()
                );
            }

            reader.Close();
            cmd.Dispose();

            if (orderIds.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No delivered orders to evaluate.[/]");
                return;
            }

            AnsiConsole.MarkupLine("[bold green]Eligible orders for evaluation:[/]");
            AnsiConsole.Write(table);

            int orderId = -1;
            while (!orderIds.Contains(orderId))
            {
                orderId = AnsiConsole.Ask<int>("Enter the [green]Order ID[/] to evaluate (0 to cancel):");
                if (orderId == 0) return;

                if (!orderIds.Contains(orderId))
                    Shell.PrintError("Invalid ID or already evaluated.");
            }

            int rating = 0;
            while (rating < 1 || rating > 5)
            {
                rating = AnsiConsole.Ask<int>("Enter a [green]rating[/] from 1 to 5:");
                if (rating < 1 || rating > 5) Shell.PrintWarning("Rating must be between 1 and 5.");
            }

            string comment = AnsiConsole.Ask<string>("Enter an optional [blue]comment[/] (or press Enter to skip):");

            MySqlCommand insertCmd = new MySqlCommand(@"
            INSERT INTO evaluations (commande_id, note, commentaire)
            VALUES (@oid, @note, @comment);",
            _sqlService.GetConnection());

            insertCmd.Parameters.AddWithValue("@oid", orderId);
            insertCmd.Parameters.AddWithValue("@note", rating);
            insertCmd.Parameters.AddWithValue("@comment", string.IsNullOrWhiteSpace(comment) ? DBNull.Value : comment);
            insertCmd.ExecuteNonQuery();
            insertCmd.Dispose();

            AnsiConsole.MarkupLine($"[green]Evaluation saved for order #{orderId} with rating {rating}.[/]");
        }
        private Dictionary<int, string> GetAverageRatings()
        {
            Dictionary<int, string> ratings = new Dictionary<int, string>();

            MySqlCommand cmd = new MySqlCommand(@"
            SELECT p.plat_id, AVG(e.note) AS moyenne
            FROM plats p
            JOIN commandes c ON c.plat_id = p.plat_id
            JOIN evaluations e ON e.commande_id = c.commande_id
            GROUP BY p.plat_id;", _sqlService.GetConnection());

            MySqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                int platId = reader.GetInt32("plat_id");

                if (reader.IsDBNull(reader.GetOrdinal("moyenne")))
                {
                    ratings[platId] = "Pas encore noté";
                }
                else
                {
                    double moyenne = reader.GetDouble("moyenne");
                    ratings[platId] = $"{moyenne:0.0} / 5";
                }
            }

            reader.Close();
            cmd.Dispose();

            return ratings;
        }
    }
}
