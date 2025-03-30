using Spectre.Console;
using ParisShell.Services;
using MySql.Data.MySqlClient;

namespace ParisShell.Commands
{
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
                Shell.PrintWarning("Usage: edit [name|lastname|address|phone|password|full]");
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

        private void UpdateFull(int id)
        {
            var prenom = Ask("New first name");
            var nom = Ask("New last name");
            var adresse = Ask("New address");
            var tel = Ask("Phone");
            var mdp = AskSecret("New password");

            using var cmd = new MySqlCommand(@"
                UPDATE users 
                SET nom=@n, prenom=@p, adresse=@a, telephone=@t, mdp=@m 
                WHERE user_id=@id", _sqlService.GetConnection());
            cmd.Parameters.AddWithValue("@n", nom);
            cmd.Parameters.AddWithValue("@p", prenom);
            cmd.Parameters.AddWithValue("@a", adresse);
            cmd.Parameters.AddWithValue("@t", tel);
            cmd.Parameters.AddWithValue("@m", mdp);
            cmd.Parameters.AddWithValue("@id", id);

            int rows = cmd.ExecuteNonQuery();
            if (rows > 0)
                Shell.PrintSucces("User successfully updated.");
            else
                Shell.PrintWarning("No update performed.");
        }

        private string Ask(string label) => AnsiConsole.Ask<string>($"[blue]{label} :[/]");
        private string AskSecret(string label) => AnsiConsole.Prompt(new TextPrompt<string>($"[red]{label} :[/]").Secret(' '));
    }
}
