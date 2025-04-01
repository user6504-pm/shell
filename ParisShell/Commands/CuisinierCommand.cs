using Spectre.Console;
using MySql.Data.MySqlClient;
using ParisShell.Services;

namespace ParisShell.Commands
{
    internal class CuisinierCommand : ICommand
    {
        public string Name => "cook";
        private readonly SqlService _sqlService;
        private readonly Session _session;

        public CuisinierCommand(SqlService sqlService, Session session)
        {
            _sqlService = sqlService;
            _session = session;
        }

        public void Execute(string[] args)
        {
            if (!_session.IsAuthenticated || !_session.IsInRole("CUISINIER"))
            {
                Shell.PrintError("Access restricted to cooks only.");
                return;
            }

            if (args.Length == 0)
            {
                Shell.PrintWarning("Usage: cook [clients|stats|dishoftheday|sales|dishes|newdish|addquantity]");
                return;
            }

            switch (args[0])
            {
                case "clients":
                    ShowClients();
                    break;
                case "stats":
                    ShowPlatsStats();
                    break;
                case "dishoftheday":
                    ShowPlatDuJour();
                    break;
                case "sales":
                    ShowTotalVentesParPlat();
                    break;
                case "dishes":
                    ShowDishes();
                    break;
                case "newdish":
                    AddDish();
                    break;
                case "addquantity":
                    AddQ();
                    break;
                default:
                    Shell.PrintError("Unknown subcommand.");
                    break;
            }
        }

        private void ShowClients()
        {
            var filter = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("View served clients:")
                    .AddChoices("Since beginning", "By date range"));

            string dateCondition = "";
            var parameters = new Dictionary<string, object> {
                { "@_id", _session.CurrentUser!.Id }
            };

            if (filter == "By date range")
            {
                var from = AnsiConsole.Ask<string>("Start date (YYYY-MM-DD):");
                var to = AnsiConsole.Ask<string>("End date (YYYY-MM-DD):");
                dateCondition = "AND c.date_commande BETWEEN @from AND @to";
                parameters["@from"] = from;
                parameters["@to"] = to;
            }

            string query = $@"
                SELECT DISTINCT u.nom, u.prenom, u.email
                FROM commandes c
                JOIN plats p ON c.plat_id = p.plat_id
                JOIN clients cl ON c.client_id = cl.client_id
                JOIN users u ON cl.client_id = u.user_id
                WHERE p.user_id = @_id {dateCondition}
                ORDER BY u.nom ASC";

            _sqlService.ExecuteAndDisplay(query, parameters);
        }

        private void ShowPlatsStats()
        {
            string query = @"
                SELECT type_plat AS 'Dish Type', COUNT(*) AS 'Count'
                FROM plats
                WHERE user_id = @_id
                GROUP BY type_plat
                ORDER BY Count DESC";

            var parameters = new Dictionary<string, object> {
                { "@_id", _session.CurrentUser!.Id }
            };

            _sqlService.ExecuteAndDisplay(query, parameters);
        }

        private void ShowPlatDuJour()
        {
            string query = @"
                SELECT plat_id AS 'ID', type_plat AS 'Type',
                       nb_personnes AS 'People', 
                       CONCAT(prix_par_personne, '€') AS 'Price'
                FROM plats
                WHERE user_id = @_id AND date_fabrication = CURDATE()";

            var parameters = new Dictionary<string, object> {
                { "@_id", _session.CurrentUser!.Id }
            };

            _sqlService.ExecuteAndDisplay(query, parameters);
        }

        private void ShowTotalVentesParPlat()
        {
            string query = @"
                SELECT p.plat_id AS 'Dish ID',
                       p.type_plat AS 'Type',
                       SUM(c.quantite) AS 'Qty Sold',
                       CONCAT(FORMAT(SUM(c.quantite * p.prix_par_personne), 2), '€') AS 'Total Sales (€)'
                FROM commandes c
                JOIN plats p ON p.plat_id = c.plat_id
                WHERE p.user_id = @_id
                GROUP BY p.plat_id, p.type_plat
                ORDER BY SUM(c.quantite * p.prix_par_personne) DESC";

            var parameters = new Dictionary<string, object> {
                { "@_id", _session.CurrentUser!.Id }
            };

            _sqlService.ExecuteAndDisplay(query, parameters);
        }
        private void ShowDishes()
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[green]Your dishes :[/]");
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
                AnsiConsole.MarkupLine("[yellow]You have no dishes at the moment.[/]");
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

        }
        private void AddQ()
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
        private void AddDish()
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[green]Add a new dish[/]");

            string type = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select the [green]dish type[/]:")
                    .AddChoices("ENTREE", "PLAT PRINCIPAL", "DESSERT"));

            string nationalite = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select the [green]dish type[/]:")
                    .AddChoices("Japonaise", "Française", "Indienne", "Chinoise", "Espagnole", "Italienne","Mexicaine", "Thaïlandaise", "Coréenne", "Grecque", "Marocaine", "Vietnamienne", "Turque", "Libanaise"));
            decimal prix = AnsiConsole.Ask<decimal>("Enter the [green]price per person[/] (e.g. 12.50):");
            int quantite = AnsiConsole.Ask<int>("Enter the [green]initial quantity[/] of the dish:");
            int nbPersonnes = AnsiConsole.Ask<int>("Enter the [green]number of people[/] the dish serves:");

            DateTime dateFabrication = DateTime.Today;
            DateTime datePeremption = AnsiConsole.Ask<DateTime>("Enter the [green]expiration date[/] (YYYY-MM-DD):");

            string regime = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select the [green]dish type[/]:")
                    .AddChoices("Végan", "Végétarien", "Halal", "Kasher", "Sans gluten", "Sans lactose","Faible en glucides", "Keto", "Paléo"));
            string ingredients = AnsiConsole.Ask<string>("Enter the [green]ingredients[/] (optional, press Enter to skip):");
            string photo = AnsiConsole.Ask<string>("Enter the [green]photo URL[/] (optional, type Enter to skip):");

            string query = @"
        INSERT INTO plats 
        (user_id, type_plat, nationalite, prix_par_personne, quantite, nb_personnes, 
         date_fabrication, date_peremption, regime_alimentaire, ingredients, photo)
        VALUES 
        (@uid, @type, @nat, @prix, @quant, @nbPers, @fab, @peremp, @regime, @ingred, @photo);";

            using var cmd = new MySqlCommand(query, _sqlService.GetConnection());
            cmd.Parameters.AddWithValue("@uid", _session.CurrentUser.Id);
            cmd.Parameters.AddWithValue("@type", type);
            cmd.Parameters.AddWithValue("@nat", nationalite);
            cmd.Parameters.AddWithValue("@prix", prix);
            cmd.Parameters.AddWithValue("@quant", quantite);
            cmd.Parameters.AddWithValue("@nbPers", nbPersonnes);
            cmd.Parameters.AddWithValue("@fab", dateFabrication);
            cmd.Parameters.AddWithValue("@peremp", datePeremption);
            cmd.Parameters.AddWithValue("@regime", string.IsNullOrWhiteSpace(regime) ? DBNull.Value : regime);
            cmd.Parameters.AddWithValue("@ingred", string.IsNullOrWhiteSpace(ingredients) ? DBNull.Value : ingredients);
            cmd.Parameters.AddWithValue("@photo", string.IsNullOrWhiteSpace(photo) ? DBNull.Value : photo);

            int rows = cmd.ExecuteNonQuery();

            if (rows > 0)
            {
                AnsiConsole.MarkupLine("[green]Dish successfully added![/]");
            }
            else
            {
                Shell.PrintError("Failed to insert dish.");
            }
        }

    }
}
