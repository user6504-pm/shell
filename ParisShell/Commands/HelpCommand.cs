using ParisShell.Services;
using ParisShell;

using Spectre.Console;

/// <summary>
/// Command that provides contextual help based on the current user's roles.
/// Lists commands available to anonymous users, authenticated users, and role-specific commands.
/// </summary>
internal class HelpCommand : ICommand
{
    /// <summary>
    /// The name used to invoke the help command.
    /// </summary>
    public string Name => "help";

    private readonly SqlService _sqlService;
    private readonly Session _session;

    /// <summary>
    /// Initializes a new instance of the <see cref="HelpCommand"/> class.
    /// </summary>
    public HelpCommand(SqlService sqlService, Session session)
    {
        _sqlService = sqlService;
        _session = session;
    }

    /// <summary>
    /// Displays the appropriate commands based on the user's authentication and role.
    /// </summary>
    public void Execute(string[] args)
    {
        if (!_sqlService.IsConnected)
        {
            Shell.PrintWarning("You must be connected to access contextual help.");
            return;
        }

        var roles = _session.CurrentUser?.Roles?.Select(r => r.ToUpper()).ToList() ?? new List<string>();
        var allCommands = new Dictionary<string, List<string>>
        {
            ["ANON"] = new() { "clear", "disconnect", "login", "register" },
            ["ALL"] = new() { "clear", "disconnect", "showtables", "showtable", "logout", "deleteacc", "edit" },
            ["ADMIN"] = new() { "user add", "user update", "user assign-role", "user list", "analytics" },
            ["BOZO"] = new() { "user add", "user update", "user assign-role", "user list", "analytics" },
            ["CUISINIER"] = new() { "changerole", "cook clients", "cook stats", "cook dishoftheday", "cook sales", "cook dishes", "cook newdish","cook commands","cook verifycommands" },
            ["CLIENT"] = new() { "changerole", "client neworder", "client orders", "client cancel", "client order-travel" }
        };

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[deeppink4_2 bold]Available Commands[/]")
            .AddColumn("[white]Description[/]");

        if (!_session.IsAuthenticated)
        {
            foreach (var cmd in allCommands["ANON"])
                table.AddRow($"[white]{cmd}[/]", GetCommandDescription(cmd));
        }
        else
        {
            foreach (var cmd in allCommands["ALL"])
                table.AddRow($"[white]{cmd}[/]", GetCommandDescription(cmd));

            foreach (var role in roles)
            {
                if (allCommands.ContainsKey(role))
                {
                    foreach (var cmd in allCommands[role])
                        table.AddRow($"[green]{cmd}[/]", GetCommandDescription(cmd));
                }
            }
        }

        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Returns the description for a given command name.
    /// </summary>
    private string GetCommandDescription(string cmd) => cmd switch
    {
        "login" => "Log in to a user account.",
        "clear" => "Clear the screen.",
        "disconnect" => "Disconnect from the MySQL server.",
        "showtables" => "List accessible tables based on your role.",
        "showtable" => "Display the content of a table.",
        "logout" => "Log out of the current user account.",
        "user add" => "Add a new user (admin only).",
        "user update" => "Update a user's information.",
        "user assign-role" => "Assign a role to a user.",
        "user list" => "List and sort users.",
        "analytics" => "Show data analysis: orders, averages, client statistics.",
        "cook clients" => "List clients served by this chef.",
        "cook stats" => "View dish statistics.",
        "cook dishoftheday" => "Display today’s dish.",
        "cook sales" => "Show total sales by dish.",
        "cook verifycommands" => "Make possible to change the status of the command",
        "cook commands" => "Show the commands of the cook",
        "cook dishes" => "Show total dishes made by the cook.",
        "cook newdish" => "Create a new dish.",
        "client order-travel" => "Show the path of the order on the map.",
        "client neworder" => "Place a new order.",
        "client cancel" => "Cancel an order.",
        "client orders" => "Show the client's orders.",
        "cook addquantity" => "Add quantity to a dish.",
        "deleteacc" => "Delete the user account",
        "register" => "Register an account",
        "edit" => "Edit user profile",
        "changerole" => "Change your role with the other one available",
        _ => "No description available."
    };
}
