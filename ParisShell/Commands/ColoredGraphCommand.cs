﻿using MySql.Data.MySqlClient;
using ParisShell.Graph;
using ParisShell.Services;
using Spectre.Console;
using System.Text;

namespace ParisShell.Commands {

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
                case "g":
                    G();
                    break;
                default:
                    Shell.PrintError("Unknown subcommand.");
                    break;
            }

        }
        private void G()
        {
            var conn = _sqlService.GetConnection();

            var clientIds = new List<int>();
            var cookIds = new List<int>();
            List<(int clientId, int userId)> com = new();

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

            using (var cmd = new MySqlCommand("SELECT c.client_id, p.user_id FROM commandes c JOIN plats p ON c.plat_id = p.plat_id; ", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    com.Add((reader.GetInt32("client_id"), reader.GetInt32("user_id")));
                }
            }

            if (clientIds.Count == 0 || cookIds.Count == 0 || com.Count == 0)
            {
                Shell.PrintWarning("Missing clients, cooks or commands in the database.");
                return;
            }

            var graph = new Graph<int>();
            var noeuds = new Dictionary<int, Noeud<int>>();

            foreach (var id in clientIds.Concat(cookIds).Distinct())
            {
                var noeud = new Noeud<int>(id, id);
                graph.AjouterNoeud(noeud);
                noeuds[id] = noeud;
            }

            foreach (var (clientId, userId) in com)
            {
                if (noeuds.TryGetValue(clientId, out var clientNode) && noeuds.TryGetValue(userId, out var cookNode))
                {
                    graph.AjouterLien(clientNode, cookNode, 1);
                }
            }
            var color = graph.Welsh_Powell();

            var sb = new StringBuilder();

            sb.AppendLine("{");
            sb.AppendLine("  \"nodes\": [");

            foreach (var node in noeuds.Values) {
                var type = clientIds.Contains(node.Id) ? "client" : "cook";
                var col = color.ContainsKey(node) ? $"color_{color[node]}" : "uncolored";

                sb.AppendLine($"    {{ \"id\": {node.Id}, \"type\": \"{type}\", \"color\": \"{col}\" }},");
            }
            if (noeuds.Count > 0)
                sb.Length -= 3;

            sb.AppendLine("\n  ],");

            sb.AppendLine("  \"edges\": [");

            foreach (var lien in graph.liens) {
                sb.AppendLine($"    {{ \"source\": {lien.Noeud1.Id}, \"target\": {lien.Noeud2.Id}, \"weight\": {lien.Poids} }},");
            }
            sb.AppendLine("\n  ],");

            sb.AppendLine("  \"graph_info\": {");
            sb.AppendLine($"    \"is_directed\": false,");
            sb.AppendLine($"    \"chromatic_number\": {color.Values.Distinct().Count()},");
            sb.AppendLine($"    \"node_count\": {noeuds.Count},");
            sb.AppendLine($"    \"edge_count\": {graph.liens.Count},");
            sb.AppendLine($"    \"planar\": {graph.EstimerGraphePlanaire(color.Values.Distinct().Count()).ToString().ToLower()},");
            sb.AppendLine($"    \"bipartite\": {graph.EstimerGrapheBiparti(color.Values.Distinct().Count()).ToString().ToLower()},");
            sb.AppendLine($"    \"independent set\": {graph.TrouverGroupesIndependants(color).Keys.Count()}");
            sb.AppendLine("  }");

            File.WriteAllText("../../../../graph.json", sb.ToString());
            Shell.PrintSucces("Graph JSON written manually to graph.json");
        }
        private void InitC()
        {
            var conn = _sqlService.GetConnection();

            var clientIds = new List<int>();
            var cookIds = new List<int>();
            var plats = new List<(int platId, int userId)>();

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

                if (clientId != cookId)
                {
                    using var insert = new MySqlCommand(@"
                INSERT INTO commandes (client_id, plat_id, quantite, date_commande, statut)
                VALUES (@client, @plat, @quantite, NOW(), 'LIVREE');
            ", conn);

                    insert.Parameters.AddWithValue("@client", clientId);
                    insert.Parameters.AddWithValue("@plat", platId);
                    insert.Parameters.AddWithValue("@quantite", rand.Next(1, 4));

                    insert.ExecuteNonQuery();
                }
            }

            Shell.PrintSucces($"{count} commandes aléatoires générées avec succès.");
        }
    }
}
