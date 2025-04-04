﻿using Spectre.Console;
using MySql.Data.MySqlClient;
using ParisShell.Services;
using MySqlX.XDevAPI.CRUD;

namespace ParisShell.Commands
{
    /// <summary>
    /// Handles all commands available to cooks, including managing dishes,
    /// viewing client interactions, and generating sales statistics.
    /// </summary>
    internal class CuisinierCommand : ICommand
    {
        /// <summary>
        /// The name used to invoke the cook command.
        /// </summary>
        public string Name => "cook";
        private readonly SqlService _sqlService;
        private readonly Session _session;

        /// <summary>
        /// Initializes a new instance of the <see cref="CuisinierCommand"/> class.
        /// </summary>
        /// <param name="sqlService">The service used to interact with the database.</param>
        /// <param name="session">The current user session.</param>
        public CuisinierCommand(SqlService sqlService, Session session)
        {
            _sqlService = sqlService;
            _session = session;
        }

        /// <summary>
        /// Executes a specific subcommand for a cook, such as viewing stats, clients,
        /// managing dishes or checking today's dish.
        /// </summary>
        public void Execute(string[] args)
        {
            if (!_session.IsAuthenticated || !_session.IsInRole("CUISINIER"))
            {
                Shell.PrintError("Access restricted to cooks only.");
                return;
            }

            if (args.Length == 0)
            {
                Shell.PrintWarning("Usage: cook clients | stats | dishoftheday| sales | dishes | newdish | addquantity");
                return;
            }

            switch (args[0])
            {
                case "clients":
                    ShowClients();
                    break;
                case "stats":
                    ShowDishiesStats();
                    break;
                case "dishoftheday":
                    ShowDishOfTheDay();
                    break;
                case "sales":
                    ShowSales();
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
                case "remove":
                    Remove();
                    break;
                default:
                    Shell.PrintError("Unknown subcommand.");
                    break;
            }
        }

        /// <summary>
        /// Displays a list of clients served by the cook, either since the beginning or within a date range.
        /// </summary>
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

        /// <summary>
        /// Displays a count of each dish type the cook has created.
        /// </summary>
        private void ShowDishiesStats()
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

        /// <summary>
        /// Displays the dishes created today by the cook.
        /// </summary>
        private void ShowDishOfTheDay()
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

        /// <summary>
        /// Displays total sales and quantity sold for each dish created by the cook.
        /// </summary>
        private void ShowSales()
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

        /// <summary>
        /// Displays all the dishes created by the currently logged-in cook,
        /// including name, type, nationality, price, and quantity.
        /// </summary>
        private void ShowDishes()
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[green]Your dishes :[/]");
            List<(int Id, string Name, string Type, string Nationality, decimal Price, int Quantity)> plats = new List<(int, string, string, string, decimal, int)>();
            MySqlCommand selectCmd = new MySqlCommand(@"
                SELECT plat_id, plat_name, type_plat, nationalite, prix_par_personne, quantite
                FROM plats
                WHERE user_id = @uid;",
                _sqlService.GetConnection());

            selectCmd.Parameters.AddWithValue("@uid", _session.CurrentUser.Id);

            MySqlDataReader reader = selectCmd.ExecuteReader();

            while (reader.Read())
            {
                int id = reader.GetInt32("plat_id");
                string name = reader.GetString("plat_name");
                string type = reader.GetString("type_plat");
                string nat = reader.GetString("nationalite");
                decimal price = reader.GetDecimal("prix_par_personne");
                int quantity = reader.GetInt32("quantite");

                plats.Add((id, name, type, nat, price, quantity));
            }

            selectCmd.Dispose();
            reader.Close();

            if (plats.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]You have no dishes at the moment.[/]");
                return;
            }

            Table table = new Table().Border(TableBorder.Rounded);
            table.AddColumns("ID", "Name", "Type", "Nationality", "Price", "Quantity");

            foreach ((int id, string name, string type, string nat, decimal price, int quantity) plat in plats)
            {
                table.AddRow(
                    plat.id.ToString(),
                    plat.name,
                    plat.type,
                    plat.nat,
                    plat.price.ToString(),
                    plat.quantity.ToString()
                );
            }

            AnsiConsole.Write(table);
        }

        /// <summary>
        /// Allows the cook to add more quantity to an existing dish.
        /// Displays all dishes and prompts the user to select one and input the additional quantity.
        /// </summary>
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

            List<(int Id, string Name, string Type, string Nationality, decimal Price, int Quantity)> plats = new List<(int, string, string, string, decimal, int)>();

            MySqlCommand selectCmd = new MySqlCommand(@"
                SELECT plat_id, plat_name, type_plat, nationalite, prix_par_personne, quantite
                FROM plats
                WHERE user_id = @uid;",
                _sqlService.GetConnection());

            selectCmd.Parameters.AddWithValue("@uid", _session.CurrentUser.Id);

            MySqlDataReader reader = selectCmd.ExecuteReader();

            while (reader.Read())
            {
                int id = reader.GetInt32("plat_id");
                string name = reader.GetString("plat_name");
                string type = reader.GetString("type_plat");
                string nat = reader.GetString("nationalite");
                decimal price = reader.GetDecimal("prix_par_personne");
                int quantity = reader.GetInt32("quantite");

                plats.Add((id, name, type, nat, price, quantity));
            }

            selectCmd.Dispose();
            reader.Close();

            if (plats.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]You have no dishes to modify at the moment.[/]");
                return;
            }

            Table table = new Table().Border(TableBorder.Rounded);
            table.AddColumns("ID", "Name", "Type", "Nationality", "Price", "Quantity");

            foreach ((int id, string name, string type, string nat, decimal price, int quantity) plat in plats)
            {
                table.AddRow(
                    plat.id.ToString(),
                    plat.name,
                    plat.type,
                    plat.nat,
                    plat.price.ToString(),
                    plat.quantity.ToString()
                );
            }

            AnsiConsole.Write(table);

            (int Id, string Name, string Type, string Nationality, decimal Price, int Quantity) selectionnedDish = default;
            bool found = false;

            while (!found)
            {
                int platIdChoisi = AnsiConsole.Ask<int>("Enter the [green]ID[/] of the dish to modify:");
                int i = 0;

                while (i < plats.Count && !found)
                {
                    (int Id, string Name, string Type, string Nationalite, decimal Prix, int Quantite) plat = plats[i];

                    if (plat.Id == platIdChoisi)
                    {
                        selectionnedDish = plat;
                        found = true;
                    }

                    i++;
                }

                if (!found)
                {
                    Shell.PrintError("Dish not found. Please enter a valid ID.");
                }
            }

            int addition = AnsiConsole.Ask<int>("Enter [green]quantity to add[/]:");

            MySqlCommand updateCmd = new MySqlCommand(@"
                UPDATE plats
                SET quantite = quantite + @ajout
                WHERE plat_id = @pid;",
                _sqlService.GetConnection());

            updateCmd.Parameters.AddWithValue("@ajout", addition);
            updateCmd.Parameters.AddWithValue("@pid", selectionnedDish.Id);

            updateCmd.ExecuteNonQuery();
            updateCmd.Dispose();
        }

        /// <summary>
        /// Adds a new dish to the system with details such as name, type, nationality,
        /// price, quantity, expiration date, dietary info, and optional picture.
        /// </summary>
        private void AddDish()
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[green]Add a new dish[/]");

            string name = AnsiConsole.Ask<string>("Enter the [green]dish name[/]:");

            string type = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select the [green]dish type[/]:")
                    .AddChoices("ENTREE", "PLAT PRINCIPAL", "DESSERT"));

            string nationality = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select the [green]dish type[/]:")
                    .AddChoices("Japonaise", "Française", "Indienne", "Chinoise", "Espagnole", "Italienne", "Mexicaine", "Thaïlandaise", "Coréenne", "Grecque", "Marocaine", "Vietnamienne", "Turque", "Libanaise"));
            decimal price = AnsiConsole.Ask<decimal>("Enter the [green]price per person[/] (e.g. 12.50):");
            int quantity = AnsiConsole.Ask<int>("Enter the [green]initial quantity[/] of the dish:");
            int numberPeople = AnsiConsole.Ask<int>("Enter the [green]number of people[/] the dish serves:");

            DateTime FabricationDate = DateTime.Today;
            DateTime ExpiryDate = AnsiConsole.Ask<DateTime>("Enter the [green]expiration date[/] (YYYY-MM-DD):");

            string diet = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select the [green]dish type[/]:")
                    .AddChoices("Aucun", "Végan", "Végétarien", "Halal", "Kasher", "Sans gluten", "Sans lactose", "Faible en glucides", "Keto", "Paléo"));
            string ingredients = AnsiConsole.Ask<string>("Enter the [green]ingredients[/] (optional, press Enter to skip):");
            string picture = AnsiConsole.Ask<string>("Enter the [green]photo URL[/] (optional, type Enter to skip):");

            string query = @"
            INSERT INTO plats 
            (user_id, plat_name, type_plat, nationalite, prix_par_personne, quantite, nb_personnes, 
            date_fabrication, date_peremption, regime_alimentaire, ingredients, photo)
            VALUES 
            (@uid, @name, @type, @nat, @prix, @quant, @nbPers, @fab, @peremp, @regime, @ingred, @photo);";

            MySqlCommand cmd = new MySqlCommand(query, _sqlService.GetConnection());
            cmd.Parameters.AddWithValue("@uid", _session.CurrentUser.Id);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@type", type);
            cmd.Parameters.AddWithValue("@nat", nationality);
            cmd.Parameters.AddWithValue("@prix", price);
            cmd.Parameters.AddWithValue("@quant", quantity);
            cmd.Parameters.AddWithValue("@nbPers", numberPeople);
            cmd.Parameters.AddWithValue("@fab", FabricationDate);
            cmd.Parameters.AddWithValue("@peremp", ExpiryDate);
            cmd.Parameters.AddWithValue("@regime", string.IsNullOrWhiteSpace(diet) ? DBNull.Value : diet);
            cmd.Parameters.AddWithValue("@ingred", string.IsNullOrWhiteSpace(ingredients) ? DBNull.Value : ingredients);
            cmd.Parameters.AddWithValue("@photo", string.IsNullOrWhiteSpace(picture) ? DBNull.Value : picture);

            cmd.ExecuteNonQuery();
            cmd.Dispose();
        }

        /// <summary>
        /// Removes a dish created by the cook after confirming ownership and deletion.
        /// </summary>
        private void Remove()
        {
            AnsiConsole.Clear();
            ShowDishes();

            int IdDish = -1;
            string DishName = "";
            bool found = false;

            while (!found)
            {
                int ChosenIdDish = AnsiConsole.Ask<int>("Enter the [red]Dish ID[/] you want to delete:");

                string checkQuery = @"
                 SELECT plat_id, plat_name 
                 FROM plats 
                 WHERE plat_id = @pid AND user_id = @uid";

                MySqlCommand checkCmd = new MySqlCommand(checkQuery, _sqlService.GetConnection());
                checkCmd.Parameters.AddWithValue("@pid", ChosenIdDish);
                checkCmd.Parameters.AddWithValue("@uid", _session.CurrentUser.Id);

                MySqlDataReader reader = checkCmd.ExecuteReader();
                if (reader.Read())
                {
                    IdDish = reader.GetInt32("plat_id");
                    DishName = reader.GetString("plat_name");
                    found = true;
                }
                else
                {
                    Shell.PrintError("Invalid dish ID or it does not belong to you. Try again.");
                }

                reader.Close();
                checkCmd.Dispose();
            }

            string confirmation = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"Are you sure you want to delete dish [red]{DishName}[/] (ID {IdDish}) ?")
                    .AddChoices("Yes", "No")
            );
            bool confirm = true;
            if (confirmation == "No")
            {
                AnsiConsole.MarkupLine("[yellow]Deletion aborted by the user.[/]");
                return;
            }

            string deleteQuery = @"
                DELETE FROM plats
                WHERE plat_id = @pid AND user_id = @uid";

            MySqlCommand deleteCmd = new MySqlCommand(deleteQuery, _sqlService.GetConnection());
            deleteCmd.Parameters.AddWithValue("@pid", IdDish);
            deleteCmd.Parameters.AddWithValue("@uid", _session.CurrentUser.Id);

            deleteCmd.ExecuteNonQuery();
            deleteCmd.Dispose();
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[yellow]Dish removed.[/]");
        }
    }
}



