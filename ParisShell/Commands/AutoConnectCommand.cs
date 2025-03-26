using Spectre.Console;
using ParisShell.Services;
using MySql.Data.MySqlClient;
using ParisShell.Models;

namespace ParisShell.Commands {
    internal class AutoConnectCommand : ICommand {
        public string Name => "autoconnect";

        private readonly SqlService _sqlService;
        private readonly Session _session;

        public AutoConnectCommand(SqlService sqlService, Session session) {
            _sqlService = sqlService;
            _session = session;
        }

        public void Execute(string[] args) {
            // 1. Connexion MySQL
            var config = new SqlConnectionConfig {
                SERVER = "localhost",
                PORT = "3306",
                DATABASE = "livininparis_219",
                UID = "root",
                PASSWORD = "Gandalf"
            };

            if (!_sqlService.Connect(config)) {
                AnsiConsole.MarkupLine("[red]⛔ Échec de la connexion MySQL.[/]");
                return;
            }
            AnsiConsole.MarkupLine("[green]✅ Connecté à MySQL avec succès.[/]");

            // 2. Login applicatif
            string email = "Mdupond@gmail.com";
            string mdp = "hashedmdp"; // à adapter si hashage à comparer

            try {
                string userQuery = @"
                    SELECT user_id, nom, prenom
                    FROM users
                    WHERE email = @mail AND mdp = @pwd";

                using var cmd = new MySqlCommand(userQuery, _sqlService.GetConnection());
                cmd.Parameters.AddWithValue("@mail", email);
                cmd.Parameters.AddWithValue("@pwd", mdp);

                using var reader = cmd.ExecuteReader();
                if (!reader.Read()) {
                    AnsiConsole.MarkupLine("[red]⛔ Identifiants invalides.[/]");
                    return;
                }

                var user = new Models.User {
                    Id = reader.GetInt32("user_id"),
                    Nom = reader.GetString("nom"),
                    Prenom = reader.GetString("prenom"),
                    Email = email
                };
                reader.Close();

                // Rôles
                string roleQuery = @"SELECT r.role_name
                                     FROM user_roles ur
                                     JOIN roles r ON r.role_id = ur.role_id
                                     WHERE ur.user_id = @uid";

                using var roleCmd = new MySqlCommand(roleQuery, _sqlService.GetConnection());
                roleCmd.Parameters.AddWithValue("@uid", user.Id);

                using var roleReader = roleCmd.ExecuteReader();
                while (roleReader.Read()) {
                    user.Roles.Add(roleReader.GetString("role_name"));
                }

                _session.CurrentUser = user;

                AnsiConsole.MarkupLine($"[green]✅ Connecté en tant que [bold]{user.Prenom} {user.Nom}[/] ([blue]{string.Join(", ", user.Roles)}[/])[/]");
            }
            catch (Exception ex) {
                AnsiConsole.MarkupLine($"[red]⛔ Erreur pendant le login : {ex.Message}[/]");
            }
        }
    }
}
