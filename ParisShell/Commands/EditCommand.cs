﻿using MySql.Data.MySqlClient;
using ParisShell.Services;
using ParisShell;

using Spectre.Console;

/// <summary>
/// Command allowing users to edit their personal account information.
/// </summary>
internal class EditCommand : ICommand
{
    /// <summary>
    /// The command name used in the shell.
    /// </summary>
    public string Name => "edit";

    private readonly SqlService _sqlService;
    private readonly Session _session;

    /// <summary>
    /// Initializes the EditCommand with required services.
    /// </summary>
    public EditCommand(SqlService sqlService, Session session)
    {
        _sqlService = sqlService;
        _session = session;
    }

    /// <summary>
    /// Executes the user information update process based on provided arguments.
    /// </summary>
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
                UpdateField(userId, "prenom", Ask("New first name"));
                break;
            case "lastname":
                UpdateField(userId, "nom", Ask("New last name"));
                break;
            case "address":
                UpdateField(userId, "adresse", Ask("New address"));
                break;
            case "phone":
                UpdateField(userId, "telephone", Ask("New phone"));
                break;
            case "password":
                UpdateField(userId, "mdp", AskSecret("New password"));
                break;
            case "full":
                UpdateFull(userId);
                break;
            default:
                Shell.PrintError("Unknown edit field.");
                break;
        }
    }

    /// <summary>
    /// Updates a specific field in the user's row in the database.
    /// </summary>
    private void UpdateField(int id, string field, string value)
    {
        string query = $"UPDATE users SET {field} = @val WHERE user_id = @id";
        using var cmd = new MySqlCommand(query, _sqlService.GetConnection());
        cmd.Parameters.AddWithValue("@val", value);
        cmd.Parameters.AddWithValue("@id", id);
        int rows = cmd.ExecuteNonQuery();
        if (rows > 0)
            Shell.PrintSucces($"Field '{field}' successfully updated.");
        else
            Shell.PrintWarning("No update performed.");
    }

    /// <summary>
    /// Prompts the user to update all editable fields at once.
    /// </summary>
    private void UpdateFull(int id)
    {
        var firstname = Ask("New first name");
        var lastname = Ask("New last name");
        var adress = Ask("New address");
        var phone = Ask("Phone");
        var pwd = AskSecret("New password");

        using var cmd = new MySqlCommand(@"
            UPDATE users 
            SET nom=@n, prenom=@p, adresse=@a, telephone=@t, mdp=@m 
            WHERE user_id=@id", _sqlService.GetConnection());
        cmd.Parameters.AddWithValue("@n", firstname);
        cmd.Parameters.AddWithValue("@p", lastname);
        cmd.Parameters.AddWithValue("@a", adress);
        cmd.Parameters.AddWithValue("@t", phone);
        cmd.Parameters.AddWithValue("@m", pwd);
        cmd.Parameters.AddWithValue("@id", id);

        int rows = cmd.ExecuteNonQuery();
        if (rows > 0)
            Shell.PrintSucces("User successfully updated.");
        else
            Shell.PrintWarning("No update performed.");
    }

    /// <summary>
    /// Prompts the user for visible input.
    /// </summary>
    private string Ask(string label) => AnsiConsole.Ask<string>($"[blue]{label} :[/]");

    /// <summary>
    /// Prompts the user for hidden (secret) input.
    /// </summary>
    private string AskSecret(string label) => AnsiConsole.Prompt(new TextPrompt<string>($"[red]{label} :[/]").Secret(' '));
}
