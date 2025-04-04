using Spectre.Console;
using ParisShell.Services;
using MySql.Data.MySqlClient;
using System;

namespace ParisShell.Commands {
    internal class DeleteAccCommand : ICommand {
        public string Name => "deleteacc";

        private readonly SqlService _sqlService;
        private readonly Session _session;

        public DeleteAccCommand(SqlService sqlService, Session session) {
            _sqlService = sqlService;
            _session = session;
        }

        public void Execute(string[] args) {
            if (!_session.IsAuthenticated) {
                Shell.PrintError("You must be logged in to delete your account.");
                return;
            }

            string confirmation = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold red]Are you sure you want to permanently delete your account?[/]")
                    .AddChoices("Yes", "No")
            );

            if (confirmation == "No") {
                AnsiConsole.MarkupLine("[yellow]Account deletion cancelled.[/]");
                return;
            }

            string password = AnsiConsole.Prompt(
                new TextPrompt<string>("[red]Please re-enter your password[/]:")
                    .PromptStyle("red")
                    .Secret(' ')
            );

            try {
                using var checkCmd = new MySqlCommand(@"
                    SELECT COUNT(*) 
                    FROM users 
                    WHERE user_id = @uid AND mdp = @pwd;",
                    _sqlService.GetConnection());

                checkCmd.Parameters.AddWithValue("@uid", _session.CurrentUser.Id);
                checkCmd.Parameters.AddWithValue("@pwd", password);

                long count = (long)checkCmd.ExecuteScalar();
                if (count == 0) {
                    Shell.PrintError("Incorrect password. Account not deleted.");
                    return;
                }

                using var deleteCmd = new MySqlCommand(@"
                    DELETE FROM users 
                    WHERE user_id = @uid;",
                    _sqlService.GetConnection());

                deleteCmd.Parameters.AddWithValue("@uid", _session.CurrentUser.Id);
                deleteCmd.ExecuteNonQuery();

                Shell.PrintSucces("Account deleted successfully.");
                _session.CurrentUser = null;
            }
            catch (Exception ex) {
                Shell.PrintError($"An error occurred: {ex.Message}");
            }
        }
    }
}
