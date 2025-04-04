using Spectre.Console;
using ParisShell.Services;

namespace ParisShell.Commands
{

    /// <summary>
    /// Command to log out the currently authenticated user.
    /// </summary>
    internal class LogoutCommand : ICommand
    {

        /// <summary>
        /// Command name used in the shell.
        /// </summary>
        public string Name => "logout";

        private readonly Session _session;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogoutCommand"/> class.
        /// </summary>
        /// <param name="session">The current user session.</param>
        public LogoutCommand(Session session)
        {
            _session = session;
        }

        /// <summary>
        /// Executes the logout operation and clears the session's user context.
        /// </summary>
        /// <param name="args">Unused command-line arguments.</param>
        public void Execute(string[] args)
        {
            if (!_session.IsAuthenticated)
            {
                Shell.PrintWarning("No user is currently logged in.");
                return;
            }

            string user = _session.CurrentUser?.Email ?? "unknown";
            _session.CurrentUser = null;
            Shell.PrintSucces($"User [bold]{user}[/] has been successfully logged out.");
        }
    }
}

