using Spectre.Console;
using ParisShell.Services;
using ParisShell.Models;

namespace ParisShell.Commands {
    internal class ConnectCommand : ICommand {
        private readonly SqlService _sqlService;
        public string Name => "connect";

        public ConnectCommand(SqlService sqlService) {
            _sqlService = sqlService;
        }

        public void Execute(string[] args) {
            if (_sqlService.IsConnected) {
                Shell.PrintWarning("Already connected.");
                return;
            }

            try
            {
                Console.CursorVisible = false;

                string mdp = AnsiConsole.Prompt(
                    new TextPrompt<string>("MySQL password [grey](root)[/]:")
                        .PromptStyle("red")
                        .Secret(' ')
                );

                var config = new SqlConnectionConfig
                {
                    SERVER = "localhost",
                    PORT = "3306",
                    UID = "root",
                    DATABASE = "Livininparis_219",
                    PASSWORD = mdp
                };

                _sqlService.Connect(config);
            }
            finally
            {
                Console.CursorVisible = true;
            }
        }
    }
}
