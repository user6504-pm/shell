using Spectre.Console;
using ParisShell.Services;

namespace ParisShell.Commands
{

    /// <summary>
    /// Command to disconnect from the MySQL database and log out the current user.
    /// </summary>
    internal class DisconnectCommand : ICommand
    {

        private readonly SqlService _sqlService;
        private readonly Session _session;

        /// <summary>
        /// The name used to invoke this command in the shell.
        /// </summary>
        public string Name => "disconnect";

        /// <summary>
        /// Initializes a new instance of the <see cref="DisconnectCommand"/> class.
        /// </summary>
        /// <param name="sqlService">SQL service to manage connection state.</param>
        /// <param name="_sesssion">Current session object holding the logged-in user.</param>
        public DisconnectCommand(SqlService sqlService, Session _sesssion)
        {
            _sqlService = sqlService;
            _session = _sesssion;
        }

        /// <summary>
        /// Executes the disconnect command.
        /// Disconnects from MySQL and clears the current user session if needed.
        /// </summary>
        /// <param name="args">Command-line arguments (not used).</param>
        public void Execute(string[] args)
        {
            if (!_sqlService.IsConnected)
            {
                Shell.PrintWarning("Not connected: nothing to disconnect from.");
                return;
            }

            _sqlService.Disconnect();
            Shell.PrintSucces("Disconnected from MySQL.");

            if (_session.CurrentUser != null)
            {
                string user = _session.CurrentUser.Email;
                _session.CurrentUser = null;
                Shell.PrintSucces($"User [bold]{user}[/] logged out.");
            }
        }
    }
}
