﻿using Spectre.Console;
using ParisShell.Services;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using ParisShell.Models;

namespace ParisShell.Commands {
    internal class LoginCommand : ICommand {
        private readonly SqlService _sqlService;
        private readonly Session _session;

        public string Name => "login";

        public LoginCommand(SqlService sqlService, Session session) {
            _sqlService = sqlService;
            _session = session;
        }

        public void Execute(string[] args) {
            string email = AnsiConsole.Prompt(
                new TextPrompt<string>("Email:")
                    .PromptStyle("blue"));

            string password = AnsiConsole.Prompt(
                new TextPrompt<string>("Password:")
                    .PromptStyle("red")
                    .Secret(' '));

            try {
                string userQuery = @"
                    SELECT user_id, nom, prenom
                    FROM users
                    WHERE email = @email AND mdp = @pwd";

                using var cmd = new MySqlCommand(userQuery, _sqlService.GetConnection());
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@pwd", password);

                using var reader = cmd.ExecuteReader();
                if (!reader.Read()) {
                    Shell.PrintError("Invalid credentials.");
                    return;
                }

                var user = new User {
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
                while (roleReader.Read()) {
                    user.Roles.Add(roleReader.GetString("role_name"));
                }

                _session.CurrentUser = user;

                Shell.PrintSucces($"Logged in as [bold]{user.LastName} {user.Prenom}[/] ([blue]{string.Join(", ", user.Roles)}[/])");
            }
            catch (Exception ex) {
                Shell.PrintError($"Login error: {ex.Message}");
            }
        }
    }
}
