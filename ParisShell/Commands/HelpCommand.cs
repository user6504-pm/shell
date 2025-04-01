using Spectre.Console;
using ParisShell.Services;

namespace ParisShell.Commands;

internal class HelpCommand : ICommand {
    public string Name => "help";

    private readonly SqlService _sqlService;
    private readonly Session _session;



    public HelpCommand(SqlService sqlService, Session session) {
        _sqlService = sqlService;
        _session = session;
    }

    public void Execute(string[] args) {
        if (!_sqlService.IsConnected) {
            Shell.PrintWarning("You must be connected to access contextual help.");
            return;
        }

        var roles = _session.CurrentUser?.Roles?.Select(r => r.ToUpper()).ToList() ?? new List<string>();
        var allCommands = new Dictionary<string, List<string>> {
            ["ANON"] = new() { "clear", "disconnect", "login" },
            ["ALL"] = new() { "clear", "disconnect", "showtables", "showtable", "logout" },
            ["ADMIN"] = new() { "user add", "user update", "user assign-role", "user list", "analytics" },
            ["BOZO"] = new() { "user add", "user update", "user assign-role", "user list", "analytics" },
            ["CUISINIER"] = new() { "cook clients", "cook stats", "cook dishoftheday", "cook sales","cook dishes","cook newdish" },
            ["CLIENT"] = new() { "showtable", "showtables" }
        };

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[deeppink4_2 bold]Available Commands[/]")
            .AddColumn("[white]Description[/]");

        if (!_session.IsAuthenticated) {
            foreach (var cmd in allCommands["ANON"])
                table.AddRow($"[white]{cmd}[/]", GetCommandDescription(cmd));
        }
        else {
            // Always accessible
            foreach (var cmd in allCommands["ALL"])
                table.AddRow($"[white]{cmd}[/]", GetCommandDescription(cmd));

            // Role-specific
            foreach (var role in roles) {
                if (allCommands.ContainsKey(role)) {
                    foreach (var cmd in allCommands[role])
                        table.AddRow($"[green]{cmd}[/]", GetCommandDescription(cmd));
                }
            }
        }
        AnsiConsole.Write(table);
    }

    private string GetCommandDescription(string cmd) => cmd switch {
        "login" => "Can login to an user account",
        "clear" => "Clears the screen.",
        "disconnect" => "Disconnects from the MySQL server.",
        "showtables" => "Lists accessible tables based on your role.",
        "showtable" => "Displays the content of a table.",
        "logout" => "Logs out from the current user.",
        "user add" => "Add a new user (admin only).",
        "user update" => "Update a user's info.",
        "user assign-role" => "Assign a role to a user.",
        "user list" => "List users and sort them.",
        "analytics" => "Show data analysis: orders, averages, client stats.",
        "cook clients" => "List clients served by this chef.",
        "cook stats" => "View dish statistics.",
        "cook dishoftheday" => "Display today’s dish.",
        "cook sales" => "Show total sales by dish.",
        "cook dishes" => "Show total dishes by the cook",
        "cook newdish " => "Create a new dish for the cook",
        _ => "No description available."
    };
}
