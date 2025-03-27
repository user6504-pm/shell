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
            var config = new SqlConnectionConfig {
                SERVER = "localhost",
                PORT = "3306",
                DATABASE = "livininparis_219",
                UID = "root",
                PASSWORD = "Gandalf"
            };

            if (!_sqlService.Connect(config)) {
                Shell.PrintError("MySQL connection failed.");
                return;
            }

            Shell.PrintSucces("Connected to MySQL.");

            string email = "bozo";
            string mdp = "Bozo1234@";

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
                    Shell.PrintError("Invalid credentials.");
                    return;
                }

                var user = new User {
                    Id = reader.GetInt32("user_id"),
                    Nom = reader.GetString("nom"),
                    Prenom = reader.GetString("prenom"),
                    Email = email
                };
                reader.Close();

                string roleQuery = @"
                    SELECT r.role_name
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

                Shell.PrintSucces($"Logged in as [bold]{user.Prenom} {user.Nom}[/] ([blue]{string.Join(", ", user.Roles)}[/])");
            }
            catch (Exception ex) {
                Shell.PrintError($"Login error: {ex.Message}");
            }
        }
    }
}
