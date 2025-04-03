using Spectre.Console;
using MySql.Data.MySqlClient;
using ParisShell.Models;
using ParisShell.Services;

namespace ParisShell.Commands;

internal class RegisterCommand : ICommand {
    public string Name => "register";
    private readonly SqlService _sqlService;

    public RegisterCommand(SqlService sqlService) {
        _sqlService = sqlService;
    }

    public void Execute(string[] args) {
        if (!_sqlService.IsConnected) {
            Shell.PrintError("You must be connected to MySQL first.");
            return;
        }

        AnsiConsole.MarkupLine("[bold underline deeppink4_2]User Registration[/]");

        string firstname = Ask("First name");
        string lastname = Ask("Last name");
        string adress = Ask("Address");
        string phone = Ask("Phone");
        string email = Ask("Email");
        string pwd = AnsiConsole.Prompt(new TextPrompt<string>("Password:")
                                        .PromptStyle("red").Secret(' '));
        int metro = AnsiConsole.Ask<int>("Closest metro station ID:");

        int userId;
        using (var cmd = new MySqlCommand(
            "INSERT INTO users (nom, prenom, adresse, telephone, email, mdp, metroproche) VALUES (@n, @p, @a, @t, @e, @m, @mp); SELECT LAST_INSERT_ID();",
            _sqlService.GetConnection())) {
            cmd.Parameters.AddWithValue("@n", firstname);
            cmd.Parameters.AddWithValue("@p", lastname);
            cmd.Parameters.AddWithValue("@a", adress);
            cmd.Parameters.AddWithValue("@t", phone);
            cmd.Parameters.AddWithValue("@e", email);
            cmd.Parameters.AddWithValue("@m", pwd);
            cmd.Parameters.AddWithValue("@mp", metro);
            userId = Convert.ToInt32(cmd.ExecuteScalar());
        }

        AssignRole(userId, "CLIENT");
        AddClientDetails(userId);

        Shell.PrintSucces("Registration successful! You can now use [green]login[/] to log in.");
    }

    private string Ask(string label) =>
        AnsiConsole.Ask<string>($"[blue]{label}:[/]");

    private void AssignRole(int userId, string role) {
        using var roleCmd = new MySqlCommand("SELECT role_id FROM roles WHERE role_name = @r", _sqlService.GetConnection());
        roleCmd.Parameters.AddWithValue("@r", role);
        int roleId = Convert.ToInt32(roleCmd.ExecuteScalar());

        using var insert = new MySqlCommand("INSERT INTO user_roles (user_id, role_id) VALUES (@u, @r)", _sqlService.GetConnection());
        insert.Parameters.AddWithValue("@u", userId);
        insert.Parameters.AddWithValue("@r", roleId);
        insert.ExecuteNonQuery();
    }

    private void AddClientDetails(int userId) {
        var type = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Client type:")
                .AddChoices("PARTICULIER", "ENTREPRISE"));

        using var cmd = new MySqlCommand("INSERT INTO clients (client_id, type_client) VALUES (@id, @type)", _sqlService.GetConnection());
        cmd.Parameters.AddWithValue("@id", userId);
        cmd.Parameters.AddWithValue("@type", type);
        cmd.ExecuteNonQuery();
    }
}
