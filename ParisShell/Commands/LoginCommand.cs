using Spectre.Console;
using ParisShell.Services;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using ParisShell.Models;

namespace ParisShell.Commands
{

    /// <summary>
    /// Command that handles user login by verifying email and password.
    /// On successful authentication, the session is populated with the user info and roles.
    /// </summary>
    internal class LoginCommand : ICommand
    {

        /// <summary>
        /// The name used to invoke this command in the shell.
        /// </summary>
        public string Name => "login";

        private readonly SqlService _sqlService;
        private readonly Session _session;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginCommand"/> class.
        /// </summary>
        /// <param name="sqlService">Service for accessing the MySQL connection.</param>
        /// <param name="session">Session that holds the current user context.</param>
        public LoginCommand(SqlService sqlService, Session session)
        {
            _sqlService = sqlService;
            _session = session;
        }

        /// <summary>
        /// Executes the login logic.
        /// Prompts the user for email and password, checks credentials against the database,
        /// and loads the user's roles into the session if authenticated.
        /// </summary>
        /// <param name="args">Command-line arguments (not used).</param>
        public void Execute(string[] args)
        {
            if (!_sqlService.IsConnected)
            {
                Shell.PrintError("You must be logged to login.");
                return;
            }

            string email = AnsiConsole.Prompt(
                new TextPrompt<string>("Email:")
                    .PromptStyle("blue"));

            Console.CursorVisible = false;
            string password = AnsiConsole.Prompt(
                new TextPrompt<string>("Password:")
                    .PromptStyle("red")
                    .Secret(' '));
            Console.CursorVisible = true;

            try
            {
                string userQuery = @"
                    SELECT user_id, nom, prenom
                    FROM users
                    WHERE email = @email AND mdp = @pwd";

                using var cmd = new MySqlCommand(userQuery, _sqlService.GetConnection());
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@pwd", password);

                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                {
                    Shell.PrintError("Invalid credentials.");
                    return;
                }

                var user = new User
                {
                    Id = reader.GetInt32("user_id"),
                    LastName = reader.GetString("nom"),
                    FirstName = reader.GetString("prenom"),
                    Email = email
                };

                reader.Close();

                string roleQuery = @"
                    SELECT r.role_name
                    FROM user_roles ur
                    JOIN roles r ON r.role_id = ur.role_id
                    WHERE ur.user_id = @userId";

                using var roleCmd = new MySqlCommand(roleQuery, _sqlService.GetConnection());
                roleCmd.Parameters.AddWithValue("@userId", user.Id);

                using var roleReader = roleCmd.ExecuteReader();
                while (roleReader.Read())
                {
                    user.Roles.Add(roleReader.GetString("role_name"));
                }

                _session.CurrentUser = user;

                Shell.PrintSucces($"Logged in as [bold]{user.LastName} {user.FirstName}[/] ([blue]{string.Join(", ", user.Roles)}[/])");
            }
            catch (Exception ex)
            {
                Shell.PrintError($"Login error: {ex.Message}");
            }
        }
    }
}
