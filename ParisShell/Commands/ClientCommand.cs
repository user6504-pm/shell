using MySql.Data.MySqlClient;
using Mysqlx.Crud;
using ParisShell.Graph;
using ParisShell.Models;
using ParisShell.Services;
using SkiaSharp;
using Spectre.Console;
using System.Diagnostics;

namespace ParisShell.Commands {
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
                Shell.PrintWarning("Usage: client newc | orders | cancel | order-travel (id)");
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
                case "order-travel":
                    OrderTravel(args);
                break;
            }
        }

        private void OrderTravel(string[] args) {
            if (args.Length < 2 || !int.TryParse(args[1], out int commandeId)) {
                Shell.PrintError("Usage: client order-travel {commande_id}");
                return;
            }

            if (!_sqlService.IsConnected || !_session.IsAuthenticated) {
                Shell.PrintError("You must be connected to view travel path.");
                return;
            }

            int clientId = _session.CurrentUser.Id;

            string query = @"
        SELECT u.metroproche AS client_station, cu.metroproche AS cook_station
        FROM commandes c
        JOIN plats p ON c.plat_id = p.plat_id
        JOIN users cu ON cu.user_id = p.user_id
        JOIN users u ON u.user_id = c.client_id
        WHERE c.commande_id = @id AND c.client_id = @clientId";

            using var cmd = new MySqlCommand(query, _sqlService.GetConnection());
            cmd.Parameters.AddWithValue("@id", commandeId);
            cmd.Parameters.AddWithValue("@clientId", clientId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) {
                Shell.PrintError("Commande introuvable ou non liée à votre compte.");
                return;
            }

            int cookStationId = reader.GetInt32("cook_station");
            int clientStationId = reader.GetInt32("client_station");

            reader.Close();

            Graph<StationData> graphe = GraphLoader.Construire(_sqlService.GetConnection());
            var noeudsDict = graphe.ObtenirNoeuds().ToDictionary(n => n.Id);

            if (!noeudsDict.TryGetValue(cookStationId, out var noeudDepart) ||
                !noeudsDict.TryGetValue(clientStationId, out var noeudArrivee)) {
                Shell.PrintError("Impossible de trouver les stations associées.");
                return;
            }

            var chemin = graphe.BellmanFordCheminPlusCourt(noeudDepart, noeudArrivee);

            if (chemin == null || chemin.Count == 0) {
                Shell.PrintError("Aucun chemin trouvé entre le cuisinier et le client.");
                return;
            }

            string nomFichier = $"commande_{commandeId}_trajet.svg";
            graphe.ExporterSvg(nomFichier, chemin);
            if (File.Exists(nomFichier)) {
                Process.Start(new ProcessStartInfo {
                    FileName = nomFichier,
                    UseShellExecute = true
                });
            }
            else {
                Console.WriteLine("SVG file not found.");
            }
        }


        private int GetStationIdFromUser(int userId) {
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

        private void NewCommand()
        {
            AnsiConsole.Clear();

            if (!_sqlService.IsConnected) {
                Shell.PrintError("You must be logged to order.");
                return;
            }

            if (!_session.IsAuthenticated || !_session.IsInRole("CLIENT")) {
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

            using (MySqlDataReader reader = selectCmd.ExecuteReader()) {
                while (reader.Read()) {
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
            var noeudsDict = graph.ObtenirNoeuds().ToDictionary(n => n.Id);

            List<(int Id, string Name, string Type, string Nationalite, decimal Prix, int Quantite, decimal Temps)> platsDisponibles = new();

            foreach (var plat in platsTemp) {
                int user_station = GetStationIdFromUser(plat.UserId);
                int session_station = GetStationIdFromUser(_session.CurrentUser.Id);

                if (!noeudsDict.TryGetValue(user_station, out var noeudDepart)) continue;
                if (!noeudsDict.TryGetValue(session_station, out var noeudArrivee)) continue;

                var chemin = graph.DijkstraCheminPlusCourt(noeudDepart, noeudArrivee);
                decimal temps = graph.TempsCheminStations(chemin);

                platsDisponibles.Add((plat.PlatId, plat.Name, plat.Type, plat.Nat, plat.Prix, plat.Quantite, temps));
            }

            if (platsDisponibles.Count == 0) {
                AnsiConsole.MarkupLine("[yellow]No available dishes for the moment.[/]");
                return;
            }

            Table table = new Table().Border(TableBorder.Rounded);
            table.AddColumns("ID", "Name", "Type", "Nationality", "Price", "Quantity", "Time");

            foreach ((int id, string name, string type, string nat, decimal prix, int quantite, decimal temps) in platsDisponibles) {
                table.AddRow(
                    id.ToString(),
                    name,
                    type,
                    nat,
                    prix.ToString(),
                    quantite.ToString(),
                    temps.ToString("0") + " min"
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
                (int Id, string Name, string Type, string Nationalite, decimal Prix, int Quantite, decimal Temps) platSelectionne = default;
                bool found = false;
                int i = 0;

                while (i < platsDisponibles.Count && !found)
                {
                    (int Id, string Name, string Type, string Nationalite, decimal Prix, int Quantite, decimal Temps) plat = platsDisponibles[i];

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
    

