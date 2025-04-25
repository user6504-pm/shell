using MySql.Data.MySqlClient;
using ParisShell.Services;
using ParisShell;
using Spectre.Console;

internal class EditCommand : ICommand
{
    public string Name => "edit";
    private readonly SqlService _sqlService;
    private readonly Session _session;

    public EditCommand(SqlService sqlService, Session session)
    {
        _sqlService = sqlService;
        _session = session;
    }

    public void Execute(string[] args)
    {
        if (!_session.IsAuthenticated)
        {
            Shell.PrintError("You must be logged in to edit your information.");
            return;
        }

        if (args.Length == 0)
        {
            Shell.PrintWarning("Usage: edit name | lastname | address | phone | password | full");
            return;
        }

        int userId = _session.CurrentUser.Id;

        switch (args[0].ToLower())
        {
            case "name":
                UpdateField(userId, "prenom", AskValidatedName("New first name"));
                break;
            case "lastname":
                UpdateField(userId, "nom", AskValidatedName("New last name"));
                break;
            case "address":
                UpdateField(userId, "adresse", AskValidatedAddress("New address"));
                break;
            case "phone":
                UpdateField(userId, "telephone", AskValidatedPhone("New phone"));
                break;
            case "password":
                UpdateField(userId, "mdp", AskValidatedPassword("New password"));
                break;
            case "full":
                UpdateFull(userId);
                break;
            default:
                Shell.PrintError("Unknown edit field.");
                break;
        }
    }

    private void UpdateField(int id, string field, string value)
    {
        string query = $"UPDATE users SET {field} = @val WHERE user_id = @id";
        using var cmd = new MySqlCommand(query, _sqlService.GetConnection());
        cmd.Parameters.AddWithValue("@val", value);
        cmd.Parameters.AddWithValue("@id", id);
        int rows = cmd.ExecuteNonQuery();
        if (rows > 0)
            Shell.PrintSucces($"[green]Field '{field}' successfully updated.[/]");
        else
            Shell.PrintWarning("[yellow]No update performed.[/]");
    }

    private void UpdateFull(int id)
    {
        var firstname = AskValidatedName("New first name");
        var lastname = AskValidatedName("New last name");
        var address = AskValidatedAddress("New address");
        var phone = AskValidatedPhone("Phone");
        var pwd = AskValidatedPassword("New password");

        using var cmd = new MySqlCommand(@"
            UPDATE users 
            SET nom=@n, prenom=@p, adresse=@a, telephone=@t, mdp=@m 
            WHERE user_id=@id", _sqlService.GetConnection());
        cmd.Parameters.AddWithValue("@n", firstname);
        cmd.Parameters.AddWithValue("@p", lastname);
        cmd.Parameters.AddWithValue("@a", address);
        cmd.Parameters.AddWithValue("@t", phone);
        cmd.Parameters.AddWithValue("@m", pwd);
        cmd.Parameters.AddWithValue("@id", id);

        int rows = cmd.ExecuteNonQuery();
        if (rows > 0)
            Shell.PrintSucces("[green]User successfully updated.[/]");
        else
            Shell.PrintWarning("[yellow]No update performed.[/]");
    }

    private string AskValidatedName(string label)
    {
        string value;
        do
        {
            value = AnsiConsole.Ask<string>($"[blue]{label}:[/]");
            if (string.IsNullOrWhiteSpace(value) || !value.All(char.IsLetter))
                Shell.PrintWarning("Name must only contain letters.");
        } while (string.IsNullOrWhiteSpace(value) || !value.All(char.IsLetter));
        return value;
    }

    private string AskValidatedAddress(string label)
    {
        string value;
        do
        {
            value = AnsiConsole.Ask<string>($"[blue]{label}:[/]");
            if (!value.Any(char.IsDigit) || value.Trim().Split(' ').Length < 2)
                Shell.PrintWarning("Address must contain a street number and a street name.");
        } while (!value.Any(char.IsDigit) || value.Trim().Split(' ').Length < 2);
        return value;
    }

    private string AskValidatedPhone(string label)
    {
        string value;
        do
        {
            value = AnsiConsole.Ask<string>($"[blue]{label}:[/]");
            if (value.Length != 10 || !value.All(char.IsDigit) || !value.StartsWith("0"))
                Shell.PrintWarning("Phone number must be exactly 10 digits and start with '0'.");
        } while (value.Length != 10 || !value.All(char.IsDigit) || !value.StartsWith("0"));
        return value;
    }

    private string AskValidatedPassword(string label)
    {
        string value;
        do
        {
            value = AnsiConsole.Prompt(new TextPrompt<string>($"[red]{label}:[/]").Secret(' '));
            if (value.Length < 6)
                Shell.PrintWarning("Password must be at least 6 characters.");
        } while (value.Length < 6);
        return value;
    }
}
