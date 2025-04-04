using MySql.Data.MySqlClient;
using ParisShell.Services;
using ParisShell;

using Spectre.Console;

/// <summary>
/// Command to execute various analytics-related queries and visualizations.
/// Available to users with roles ADMIN or BOZO.
/// </summary>
internal class AnalyticsCommand : ICommand
{
    /// <summary>
    /// Command name used to trigger it in the shell.
    /// </summary>
    public string Name => "analytics";

    private readonly SqlService _sqlService;
    private readonly Session _session;

    /// <summary>
    /// Initializes the analytics command with SQL access and user session.
    /// </summary>
    public AnalyticsCommand(SqlService sqlService, Session session)
    {
        _sqlService = sqlService;
        _session = session;
    }

    /// <summary>
    /// Main entry point for the command. Validates role and executes the selected subcommand.
    /// </summary>
    public void Execute(string[] args)
    {
        if (!_session.IsAuthenticated)
        {
            Shell.PrintError("Must be logged to an account.");
            return;
        }

        if (!_session.IsInRole("ADMIN") && !_session.IsInRole("BOZO"))
        {
            Shell.PrintError("Access restricted to administrators and bozos.");
            return;
        }

        if (args.Length == 0)
        {
            Shell.PrintWarning("Usage: analytics delivery | orders | avg-price | avg-acc | client-orders");
            return;
        }

        switch (args[0])
        {
            case "delivery":
                ShowDelivery();
                break;
            case "orders":
                ShowOrders();
                break;
            case "avg-price":
                ShowAveragePrice();
                break;
            case "avg-acc":
                ShowClientPercentage();
                break;
            case "client-orders":
                ShowOrdersSorted();
                break;
            default:
                Shell.PrintError("Unknown subcommand");
                break;
        }
    }

    /// <summary>
    /// Shows the number of delivered orders grouped by cook (user who created the dish).
    /// </summary>
    private void ShowDelivery()
    {
        string query = @"
            SELECT u.nom AS 'Last Name', u.prenom AS 'First Name', COUNT(*) AS 'Deliveries'
            FROM commandes c
            JOIN plats p ON c.plat_id = p.plat_id
            JOIN users u ON p.user_id = u.user_id
            WHERE c.statut = 'LIVREE'
            GROUP BY p.user_id";

        _sqlService.ExecuteAndDisplay(query);
    }

    /// <summary>
    /// Displays all orders placed between two specified dates.
    /// </summary>
    private void ShowOrders()
    {
        var from = AnsiConsole.Ask<string>("Start date (YYYY-MM-DD):");
        var to = AnsiConsole.Ask<string>("End date (YYYY-MM-DD):");

        string query = @"
            SELECT c.commande_id AS 'ID', u.nom AS 'Last Name', u.prenom AS 'First Name', 
                   c.date_commande AS 'Date', c.quantite AS 'Quantity', c.statut AS 'Status'
            FROM commandes c
            JOIN users u ON c.client_id = u.user_id
            WHERE c.date_commande BETWEEN @from AND @to
            ORDER BY c.date_commande DESC";

        var parameters = new Dictionary<string, object> {
            { "@from", from },
            { "@to", to }
        };

        _sqlService.ExecuteAndDisplay(query, parameters);
    }

    /// <summary>
    /// Calculates and displays the average price of all orders.
    /// </summary>
    private void ShowAveragePrice()
    {
        string query = @"
            SELECT AVG(p.prix_par_personne * c.quantite) AS moyenne
            FROM commandes c
            JOIN plats p ON c.plat_id = p.plat_id";

        using var cmd = new MySqlCommand(query, _sqlService.GetConnection());
        var result = cmd.ExecuteScalar();
        AnsiConsole.MarkupLine($"[green]Average order price:[/] [bold]{result:0.00} [/]");
    }

    /// <summary>
    /// Calculates and displays the percentage of users who are clients using a bar chart.
    /// </summary>
    private void ShowClientPercentage()
    {
        string query = "SELECT COUNT(*) FROM clients";
        using var cmd = new MySqlCommand(query, _sqlService.GetConnection());
        var clientCount = Convert.ToInt32(cmd.ExecuteScalar());

        query = "SELECT COUNT(*) FROM users";
        using var cmd2 = new MySqlCommand(query, _sqlService.GetConnection());
        var totalUsers = Convert.ToInt32(cmd2.ExecuteScalar());

        double ratio = (double)clientCount / totalUsers * 100;

        var chart = new BarChart()
            .Width(60)
            .Label("[white]Client account percentage (%)[/]")
            .CenterLabel()
            .AddItem("Clients", (float)ratio, new Color(0x8B, 0x0A, 0x50))
            .AddItem("Non-clients", 100f - (float)ratio, Color.White);

        AnsiConsole.Write(chart);
    }

    /// <summary>
    /// Displays a list of orders made by a client (by email), filtered optionally by nationality and date range.
    /// </summary>
    private void ShowOrdersSorted()
    {
        string email = AnsiConsole.Ask<string>("Client email:");
        string nat = AnsiConsole.Ask<string>("Dish nationality (leave empty for all):");
        string from = AnsiConsole.Ask<string>("Start date (YYYY-MM-DD):");
        string to = AnsiConsole.Ask<string>("End date (YYYY-MM-DD):");

        string query = @"
            SELECT c.commande_id AS 'ID', c.date_commande AS 'Date', 
                   p.nationalite AS 'Nationality', p.type_plat AS 'Type', 
                   CONCAT(p.prix_par_personne, '€') AS 'Price', 
                   c.quantite AS 'Quantity'
            FROM commandes c
            JOIN plats p ON c.plat_id = p.plat_id
            JOIN users u ON c.client_id = u.user_id
            WHERE u.email = @email
              AND c.date_commande BETWEEN @from AND @to";

        var parameters = new Dictionary<string, object> {
            { "@email", email },
            { "@from", from },
            { "@to", to }
        };

        if (!string.IsNullOrWhiteSpace(nat))
        {
            query += " AND p.nationalite = @nat";
            parameters["@nat"] = nat;
        }

        _sqlService.ExecuteAndDisplay(query, parameters);
    }
}
