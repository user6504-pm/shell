using Spectre.Console;
using MySql.Data.MySqlClient;
using ParisShell.Services;

namespace ParisShell.Commands
{
    internal class CuisinierCommand : ICommand
    {
        public string Name => "cuisinier";
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
                Shell.PrintError("Access restricted to cuisiniers only.");
                return;
            }

            if (args.Length == 0)
            {
                Shell.PrintWarning("Usage: cuisinier [clients|stats|platdujour|ventes]");
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
    }
}
