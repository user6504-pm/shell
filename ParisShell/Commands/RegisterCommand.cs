using Spectre.Console;
using MySql.Data.MySqlClient;
using ParisShell.Models;
using ParisShell.Services;

namespace ParisShell.Commands;

internal class RegisterCommand : ICommand
{
    public string Name => "register";
    private readonly SqlService _sqlService;

    public RegisterCommand(SqlService sqlService)
    {
        _sqlService = sqlService;
    }

    public void Execute(string[] args)
    {
        if (!_sqlService.IsConnected)
        {
            Shell.PrintError("You must be connected to MySQL first.");
            return;
        }

        string type = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Client type:")
                .AddChoices("PARTICULIER", "ENTREPRISE"));


        if (type != "ENTREPRISE")
        {

            string role = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select your [red]role[/]:")
                    .AddChoices("CUISINIER", "CLIENT"));

            AnsiConsole.MarkupLine("[bold underline deeppink4_2]User Registration[/]");

            string firstname;
            do
            {
                firstname = AnsiConsole.Ask<string>("First name:");
                if (string.IsNullOrWhiteSpace(firstname) || !firstname.All(char.IsLetter))
                    Shell.PrintWarning("First name must only contain letters.");
            } while (string.IsNullOrWhiteSpace(firstname) || !firstname.All(char.IsLetter));

            string lastname = AnsiConsole.Ask<string>("Last name:");
            do
            {
                lastname = AnsiConsole.Ask<string>("Last name:");
                if (string.IsNullOrWhiteSpace(lastname) || !lastname.All(char.IsLetter))
                    Shell.PrintWarning("Last name must only contain letters.");
            } while (string.IsNullOrWhiteSpace(lastname) || !lastname.All(char.IsLetter));

            string address;
            do
            {
                address = AnsiConsole.Ask<string>("Address:");
                if (!address.Any(char.IsDigit) || address.Trim().Split(' ').Length < 2)
                    Shell.PrintWarning("Address must contain a street number and a street name.");
            } while (!address.Any(char.IsDigit) || address.Trim().Split(' ').Length < 2);

            string phone;
            do
            {
                phone = AnsiConsole.Ask<string>("Phone:");
                if (phone.Length != 10 || !phone.All(char.IsDigit) || !phone.StartsWith("0"))
                    Shell.PrintWarning("Phone number must be exactly 10 digits and start with '0'.");
            } while (phone.Length != 10 || !phone.All(char.IsDigit) || !phone.StartsWith("0"));


            string email;
            do
            {
                email = AnsiConsole.Ask<string>("Email:");
                if (!email.Contains("@") || !email.Contains("."))
                    Shell.PrintWarning("Invalid email format.");
            } while (!email.Contains("@") || !email.Contains("."));

            string pwd;
            do
            {
                pwd = AnsiConsole.Prompt(new TextPrompt<string>("Password:")
                    .PromptStyle("red").Secret(' '));
                if (pwd.Length < 6)
                    Shell.PrintWarning("Password must be at least 6 characters.");
            } while (pwd.Length < 6);

            int metro = AnsiConsole.Ask<int>("Closest metro station ID:");

            int userId;
            using (var cmd = new MySqlCommand(
                "INSERT INTO users (nom, prenom, adresse, telephone, email, mdp, metroproche) VALUES (@n, @p, @a, @t, @e, @m, @mp); SELECT LAST_INSERT_ID();",
                _sqlService.GetConnection()))
            {
                cmd.Parameters.AddWithValue("@n", firstname);
                cmd.Parameters.AddWithValue("@p", lastname);
                cmd.Parameters.AddWithValue("@a", address);
                cmd.Parameters.AddWithValue("@t", phone);
                cmd.Parameters.AddWithValue("@e", email);
                cmd.Parameters.AddWithValue("@m", pwd);
                cmd.Parameters.AddWithValue("@mp", metro);
                userId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            Shell.PrintSucces("Registration successful! You can now use [green]login[/] to log in.");
            AssignRole(userId, role);
            AddClientDetails(userId, type);
        }
        else
        {
            AnsiConsole.MarkupLine("[bold underline deeppink4_2]Company Registration[/]");

            string companyname;
            do
            {
                companyname = AnsiConsole.Ask<string>("Company name:");
                if (string.IsNullOrWhiteSpace(companyname) || !companyname.All(char.IsLetter))
                    Shell.PrintWarning("Company name must only contain letters.");
            } while (string.IsNullOrWhiteSpace(companyname) || !companyname.All(char.IsLetter));

            string codesiren;
            do
            {
                codesiren = AnsiConsole.Ask<string>("Siren number:");
                if (codesiren.Length != 14 || !codesiren.All(char.IsDigit))
                    Shell.PrintWarning("SIREN must be exactly 14 digits.");
            } while (codesiren.Length != 14 || !codesiren.All(char.IsDigit));

            string address;
            do
            {
                address = AnsiConsole.Ask<string>("Address:");
                if (!address.Any(char.IsDigit) || address.Trim().Split(' ').Length < 2)
                    Shell.PrintWarning("Address must contain a street number and a street name.");
            } while (!address.Any(char.IsDigit) || address.Trim().Split(' ').Length < 2);

            string phone;
            do
            {
                phone = AnsiConsole.Ask<string>("Phone:");
                if (phone.Length != 10 || !phone.All(char.IsDigit) || !phone.StartsWith("0"))
                    Shell.PrintWarning("Phone number must be exactly 10 digits and start with '0'.");
            } while (phone.Length != 10 || !phone.All(char.IsDigit) || !phone.StartsWith("0"));


            string email;
            do
            {
                email = AnsiConsole.Ask<string>("Email:");
                if (!email.Contains("@") || !email.Contains("."))
                    Shell.PrintWarning("Invalid email format.");
            } while (!email.Contains("@") || !email.Contains("."));

            string pwd;
            do
            {
                pwd = AnsiConsole.Prompt(new TextPrompt<string>("Password:")
                    .PromptStyle("red").Secret(' '));
                if (pwd.Length < 6)
                    Shell.PrintWarning("Password must be at least 6 characters.");
            } while (pwd.Length < 6);

            int metro = AnsiConsole.Ask<int>("Closest metro station ID:");

            int userId;
            using (var cmd = new MySqlCommand(
                "INSERT INTO users (nom, prenom, adresse, telephone, email, mdp, metroproche) VALUES (@n, @p, @a, @t, @e, @m, @mp); SELECT LAST_INSERT_ID();",
                _sqlService.GetConnection()))
            {
                cmd.Parameters.AddWithValue("@n", codesiren);
                cmd.Parameters.AddWithValue("@p", companyname);
                cmd.Parameters.AddWithValue("@a", address);
                cmd.Parameters.AddWithValue("@t", phone);
                cmd.Parameters.AddWithValue("@e", email);
                cmd.Parameters.AddWithValue("@m", pwd);
                cmd.Parameters.AddWithValue("@mp", metro);
                userId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            Shell.PrintSucces("Registration successful! You can now use [green]login[/] to log in.");
            AssignRole(userId, "CLIENT");
            AddClientDetails(userId, type);
        }
    }

    private void AssignRole(int userId, string role)
    {
        using var roleCmd = new MySqlCommand("SELECT role_id FROM roles WHERE role_name = @r", _sqlService.GetConnection());
        roleCmd.Parameters.AddWithValue("@r", role);
        int roleId = Convert.ToInt32(roleCmd.ExecuteScalar());

        using var insert = new MySqlCommand("INSERT INTO user_roles (user_id, role_id) VALUES (@u, @r)", _sqlService.GetConnection());
        insert.Parameters.AddWithValue("@u", userId);
        insert.Parameters.AddWithValue("@r", roleId);
        insert.ExecuteNonQuery();
    }

    private void AddClientDetails(int userId, string type)
    {
        using var cmd = new MySqlCommand("INSERT INTO clients (client_id, type_client) VALUES (@id, @type)", _sqlService.GetConnection());
        cmd.Parameters.AddWithValue("@id", userId);
        cmd.Parameters.AddWithValue("@type", type);
        cmd.ExecuteNonQuery();
    }
}
