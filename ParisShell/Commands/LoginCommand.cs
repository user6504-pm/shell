using Spectre.Console;
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
            var input = ParseArgs(args);

            if (!input.ContainsKey("email") || !input.ContainsKey("pwd")) {
                AnsiConsole.MarkupLine("[red]⛔ Utilisation : login --email <email> --pwd <motdepasse>[/]");
                return;
            }

            string email = input["email"];
            string password = input["pwd"];

            try {
                // 1. Vérifier l'utilisateur
                string userQuery = @"
                    SELECT user_id, nom, prenom
                    FROM users
                    WHERE email = @email AND mdp = @pwd";

                using var cmd = new MySqlCommand(userQuery, _sqlService.GetConnection());
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@pwd", password); // TODO: hasher

                using var reader = cmd.ExecuteReader();
                if (!reader.Read()) {
                    AnsiConsole.MarkupLine("[red]⛔ Identifiants incorrects.[/]");
                    return;
                }

                var user = new User {
                    Id = reader.GetInt32("user_id"),
                    Nom = reader.GetString("nom"),
                    Prenom = reader.GetString("prenom"),
                    Email = email
                };

                reader.Close();

                // 2. Récupérer ses rôles
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

                AnsiConsole.MarkupLine($"[green]✅ Connecté en tant que [bold]{user.Prenom} {user.Nom}[/] ([blue]{string.Join(", ", user.Roles)}[/])[/]");
            }
            catch (Exception ex) {
                AnsiConsole.MarkupLine($"[red]⛔ Erreur de connexion : {ex.Message}[/]");
            }
        }

        private Dictionary<string, string> ParseArgs(string[] args) {
            var result = new Dictionary<string, string>();
            for (int i = 0; i < args.Length; i++) {
                if ((args[i] == "--email" || args[i] == "-e") && i + 1 < args.Length)
                    result["email"] = args[++i];
                else if ((args[i] == "--pwd" || args[i] == "-p") && i + 1 < args.Length)
                    result["pwd"] = args[++i];
            }
            return result;
        }
    }
}
