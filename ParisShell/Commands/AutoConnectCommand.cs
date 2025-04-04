using Spectre.Console;
using ParisShell.Services;
using MySql.Data.MySqlClient;
using ParisShell.Models;

namespace ParisShell.Commands
{

    /// <summary>
    /// Command used to automatically connect to the MySQL database and log in a predefined user.
    /// Supports optional flags: -c (Catherine), -b (Bozo), or default user.
    /// </summary>
    internal class AutoConnectCommand : ICommand
    {

        /// <summary>
        /// Name of the command used in the shell.
        /// </summary>
        public string Name => "autoconnect";

        private readonly SqlService _sqlService;
        private readonly Session _session;

        /// <summary>
        /// Constructor to inject SQL service and session management.
        /// </summary>
        public AutoConnectCommand(SqlService sqlService, Session session)
        {
            _sqlService = sqlService;
            _session = session;
        }

        /// <summary>
        /// Executes the autoconnect command, connecting to MySQL and logging in a predefined user based on args.
        /// </summary>
        public void Execute(string[] args)
        {

            // Prevent double connection or login
            if (_session.IsAuthenticated || _sqlService.IsConnected)
            {
                Shell.PrintError("Already logged or connected.");
                return;
            }

            string email = "", password = "";

            // Select account based on provided argument flag
            if (args.Contains("-c"))
            {
                email = "catherine38@le.fr";
                password = ")*3Kx)txM(";
            }
            else if (args.Contains("-b"))
            {
                email = "bozo";
                password = "Bozo1234@";
            }
            else
            {
                email = "jbruneau@gilles.net";
                password = "$j4aoW8b01";
            }

            // SQL server configuration
            var config = new SqlConnectionConfig
            {
                SERVER = "localhost",
                PORT = "3306",
                DATABASE = "livininparis_219",
                UID = "root",
                PASSWORD = "root"
            };

            // Connect to MySQL
            if (!_sqlService.Connect(config))
            {
                Shell.PrintError("MySQL connection failed.");
                return;
            }

            Shell.PrintSucces("Connected to MySQL.");

            try
            {
                /// <summary>
                /// Query to check if the user exists with matching email and password.
                /// </summary>
                string userQuery = @"
                    SELECT user_id, nom, prenom
                    FROM users
                    WHERE email = @mail AND mdp = @pwd";

                using var cmd = new MySqlCommand(userQuery, _sqlService.GetConnection());
                cmd.Parameters.AddWithValue("@mail", email);
                cmd.Parameters.AddWithValue("@pwd", password);

                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                {
                    Shell.PrintError("Invalid credentials.");
                    return;
                }

                // Create a user object from query result
                var user = new User
                {
                    Id = reader.GetInt32("user_id"),
                    LastName = reader.GetString("nom"),
                    FirstName = reader.GetString("prenom"),
                    Email = email
                };
                reader.Close();

                /// <summary>
                /// Query to get all roles assigned to this user.
                /// </summary>
                string roleQuery = @"
                    SELECT r.role_name
                    FROM user_roles ur
                    JOIN roles r ON r.role_id = ur.role_id
                    WHERE ur.user_id = @uid";

                using var roleCmd = new MySqlCommand(roleQuery, _sqlService.GetConnection());
                roleCmd.Parameters.AddWithValue("@uid", user.Id);

                using var roleReader = roleCmd.ExecuteReader();
                while (roleReader.Read())
                {
                    user.Roles.Add(roleReader.GetString("role_name"));
                }

                // Set current session user
                _session.CurrentUser = user;

                Shell.PrintSucces($"Logged in as [bold]{user.FirstName} {user.LastName}[/] ([blue]{string.Join(", ", user.Roles)}[/])");
            }
            catch (Exception ex)
            {
                Shell.PrintError($"Login error: {ex.Message}");
            }
        }
    }
}
