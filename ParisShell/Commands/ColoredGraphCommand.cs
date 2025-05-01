using MySql.Data.MySqlClient;
using ParisShell.Services;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParisShell.Commands
{

    /// <summary>
    /// Command that trigger anything related to the graph coloration
    /// </summary>
    internal class ColoredGraphCommand : ICommand
    {

        /// <summary>
        /// Name of the command used to trigger it from the shell.
        /// </summary>
        public string Name => "cgraph";

        private readonly SqlService _sqlService;
        private readonly Session _session;

        /// <summary>
        /// Constructor that initializes the ColoredGraphCommand with necessary services.
        /// </summary>
        /// <param name="sqlService">Service used to access the MySQL database.</param>
        /// <param name="session">Session containing authentication and user info.</param>
        public ColoredGraphCommand(SqlService sqlService, Session session)
        {
            _sqlService = sqlService;
            _session = session;
        }
        public void Execute(string[] args)
        {
            if (!_session.IsAuthenticated || !_session.IsInRole("BOZO"))
            {
                Shell.PrintError("Access restricted to bozos only.");
                return;
            }
            if (args.Length == 0)
            {
                Shell.PrintWarning("Usage: cgraph ");
                return;
            }

            switch (args[0])
            {
                case "initc":
                    InitC();
                    break;
                default:
                    Shell.PrintError("Unknown subcommand.");
                    break;
            }

        }
        private void InitC()
        {
            var conn = _sqlService.GetConnection();

            var clientIds = new List<int>();
            var cookIds = new List<int>();
            var plats = new List<(int platId, int userId)>();

            // 1. Récupérer les utilisateurs CLIENT
            using (var cmd = new MySqlCommand(@"
        SELECT u.user_id FROM users u
        JOIN user_roles ur ON u.user_id = ur.user_id
        JOIN roles r ON ur.role_id = r.role_id
        WHERE r.role_name = 'CLIENT';
    ", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    clientIds.Add(reader.GetInt32("user_id"));
                }
            }

            // 2. Récupérer les utilisateurs CUISINIER
            using (var cmd = new MySqlCommand(@"
        SELECT u.user_id FROM users u
        JOIN user_roles ur ON u.user_id = ur.user_id
        JOIN roles r ON ur.role_id = r.role_id
        WHERE r.role_name = 'CUISINIER';
    ", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    cookIds.Add(reader.GetInt32("user_id"));
                }
            }

            // 3. Récupérer les plats associés à chaque cuisinier
            using (var cmd = new MySqlCommand("SELECT plat_id, user_id FROM plats", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    plats.Add((reader.GetInt32("plat_id"), reader.GetInt32("user_id")));
                }
            }

            if (clientIds.Count == 0 || cookIds.Count == 0 || plats.Count == 0)
            {
                Shell.PrintWarning("Missing clients, cooks or dishes in the database.");
                return;
            }

            int count = AnsiConsole.Ask<int>("How many fake orders do you want to generate?");

            var rand = new Random();

            for (int i = 0; i < count; i++)
            {
                int clientId = clientIds[rand.Next(clientIds.Count)];
                var (platId, cookId) = plats[rand.Next(plats.Count)];

                // Insérer commande si le cuisinier est différent du client
                if (clientId != cookId)
                {
                    using var insert = new MySqlCommand(@"
                INSERT INTO commandes (client_id, plat_id, quantite, date_commande, statut)
                VALUES (@client, @plat, @quantite, NOW(), 'LIVREE');
            ", conn);

                    insert.Parameters.AddWithValue("@client", clientId);
                    insert.Parameters.AddWithValue("@plat", platId);
                    insert.Parameters.AddWithValue("@quantite", rand.Next(1, 4)); // 1 à 3

                    insert.ExecuteNonQuery();
                }
            }

            Shell.PrintSucces($"{count} commandes aléatoires générées avec succès.");
        }
    }
}
