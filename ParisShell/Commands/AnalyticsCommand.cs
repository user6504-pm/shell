using Spectre.Console;
using MySql.Data.MySqlClient;
using ParisShell.Services;

namespace ParisShell.Commands;

internal class AnalyticsCommand : ICommand {
    public string Name => "analytics";
    private readonly SqlService _sqlService;
    private readonly Session _session;

    public AnalyticsCommand(SqlService sqlService, Session session) {
        _sqlService = sqlService;
        _session = session;
    }

    public void Execute(string[] args) {
        if(!_session.IsAuthenticated) {
            Shell.PrintError("Must be logged to an account.");
            return;
        }


        if (!_session.IsInRole("ADMIN") && !_session.IsInRole("BOZO")) {
            Shell.PrintError("Access restricted to administrators and bozos.");
            return;
        }

        if (args.Length == 0) {
            Shell.PrintWarning("Usage: analytics [delivery|orders|avg-price|avg-acc|client-orders]");
            return;
        }

        switch (args[0]) {
            case "delivery":
                ShowLivraisonsParCuisinier();
                break;
            case "orders":
                ShowCommandesParPeriode();
                break;
            case "avg-price":
                ShowAveragePrixCommandes();
                break;
            case "avg-acc":
                ShowAverageComptesClients();
                break;
            case "client-orders":
                ShowCommandesClientParNationaliteEtPeriode();
                break;
            default:
                Shell.PrintError("Unknown subcommand.");
                break;
        }
    }

    private void ShowLivraisonsParCuisinier() {
        string query = @"
            SELECT u.nom, u.prenom, COUNT(*) AS livraisons
            FROM commandes c
            JOIN plats p ON c.plat_id = p.plat_id
            JOIN users u ON p.user_id = u.user_id
            WHERE c.statut = 'LIVREE'
            GROUP BY p.user_id";

        var table = new Table().Border(TableBorder.Rounded)
            .AddColumn("Last Name")
            .AddColumn("First Name")
            .AddColumn("Deliveries");

        using var cmd = new MySqlCommand(query, _sqlService.GetConnection());
        using var reader = cmd.ExecuteReader();

        while (reader.Read()) {
            table.AddRow(reader["nom"].ToString(), reader["prenom"].ToString(), reader["livraisons"].ToString());
        }

        AnsiConsole.Write(table);
    }

    private void ShowCommandesParPeriode() {
        var from = AnsiConsole.Ask<string>("Start date (YYYY-MM-DD):");
        var to = AnsiConsole.Ask<string>("End date (YYYY-MM-DD):");

        string query = @"
            SELECT c.commande_id, u.nom, u.prenom, c.date_commande, c.quantite, c.statut
            FROM commandes c
            JOIN users u ON c.client_id = u.user_id
            WHERE c.date_commande BETWEEN @from AND @to
            ORDER BY c.date_commande DESC";

        var table = new Table().Border(TableBorder.Rounded)
            .AddColumn("ID")
            .AddColumn("Last Name")
            .AddColumn("First Name")
            .AddColumn("Date")
            .AddColumn("Quantity")
            .AddColumn("Status");

        using var cmd = new MySqlCommand(query, _sqlService.GetConnection());
        cmd.Parameters.AddWithValue("@from", from);
        cmd.Parameters.AddWithValue("@to", to);
        using var reader = cmd.ExecuteReader();

        while (reader.Read()) {
            table.AddRow(
                reader["commande_id"].ToString(),
                reader["nom"].ToString(),
                reader["prenom"].ToString(),
                reader["date_commande"].ToString(),
                reader["quantite"].ToString(),
                reader["statut"].ToString()
            );
        }

        AnsiConsole.Write(table);
    }

    private void ShowAveragePrixCommandes() {
        string query = @"
            SELECT AVG(p.prix_par_personne * c.quantite) AS moyenne
            FROM commandes c
            JOIN plats p ON c.plat_id = p.plat_id";

        using var cmd = new MySqlCommand(query, _sqlService.GetConnection());
        var result = cmd.ExecuteScalar();
        AnsiConsole.MarkupLine($"[green]💶 Average order price:[/] [bold]{result:0.00} €[/]");
    }

    private void ShowAverageComptesClients() {
        string query = "SELECT COUNT(*) FROM clients";
        using var cmd = new MySqlCommand(query, _sqlService.GetConnection());
        var count = Convert.ToInt32(cmd.ExecuteScalar());

        query = "SELECT COUNT(*) FROM users";
        using var cmd2 = new MySqlCommand(query, _sqlService.GetConnection());
        var totalUsers = Convert.ToInt32(cmd2.ExecuteScalar());

        double ratio = (double)count / totalUsers * 100;
        AnsiConsole.MarkupLine($"[blue]👥 Client account percentage:[/] [bold]{ratio:0.00}%[/]");
    }

    private void ShowCommandesClientParNationaliteEtPeriode() {
        string email = AnsiConsole.Ask<string>("Client email:");
        string nat = AnsiConsole.Ask<string>("Dish nationality (leave empty for all):");

        string query = @"
            SELECT c.commande_id, c.date_commande, p.nationalite, p.type_plat, p.prix_par_personne, c.quantite
            FROM commandes c
            JOIN plats p ON c.plat_id = p.plat_id
            JOIN users u ON c.client_id = u.user_id
            WHERE u.email = @email
              AND c.date_commande BETWEEN @from AND @to";

        if (!string.IsNullOrWhiteSpace(nat))
            query += " AND p.nationalite = @nat";

        var table = new Table().Border(TableBorder.Rounded)
            .AddColumn("ID")
            .AddColumn("Date")
            .AddColumn("Nationality")
            .AddColumn("Type")
            .AddColumn("Price")
            .AddColumn("Quantity");

        using var cmd = new MySqlCommand(query, _sqlService.GetConnection());
        cmd.Parameters.AddWithValue("@email", email);
        if (!string.IsNullOrWhiteSpace(nat))
            cmd.Parameters.AddWithValue("@nat", nat);

        using var reader = cmd.ExecuteReader();

        while (reader.Read()) {
            table.AddRow(
                reader["commande_id"].ToString(),
                reader["date_commande"].ToString(),
                reader["nationalite"].ToString(),
                reader["type_plat"].ToString(),
                $"{reader["prix_par_personne"]:0.00}€",
                reader["quantite"].ToString()
            );
        }

        AnsiConsole.Write(table);
    }
}
