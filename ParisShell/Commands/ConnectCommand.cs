using Spectre.Console;
using ParisShell.Services;
using ParisShell.Models;

namespace ParisShell.Commands
{

    /// <summary>
    /// Command to establish a connection to the MySQL database using user-provided password.
    /// </summary>
    internal class ConnectCommand : ICommand
    {

        /// <summary>
        /// Command name used in the shell interface.
        /// </summary>
        public string Name => "connect";

        private readonly SqlService _sqlService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectCommand"/> class with the SQL service dependency.
        /// </summary>
        /// <param name="sqlService">The service used to manage MySQL connections.</param>
        public ConnectCommand(SqlService sqlService)
        {
            _sqlService = sqlService;
        }

        /// <summary>
        /// Executes the connection command.
        /// Prompts the user for the root MySQL password and attempts to connect to the configured database.
        /// </summary>
        /// <param name="args">Unused. Connection is handled via interactive prompt.</param>
        public void Execute(string[] args)
        {
            if (_sqlService.IsConnected)
            {
                Shell.PrintWarning("Already connected.");
                return;
            }

            try
            {
                Console.CursorVisible = false;

                string pwd = AnsiConsole.Prompt(
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
                    PASSWORD = pwd
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
