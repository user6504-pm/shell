using Spectre.Console;
using ParisShell.Services;

namespace ParisShell.Commands {
    internal class LogoutCommand : ICommand {
        public string Name => "logout";

        private readonly Session _session;

        public LogoutCommand(Session session) {
            _session = session;
        }

        public void Execute(string[] args) {
            if (!_session.IsAuthenticated) {
                Shell.PrintWarning("No user is currently logged in.");
                return;
            }

            string user = _session.CurrentUser?.Email ?? "unknown";

            _session.CurrentUser = null;

            Shell.PrintSucces($"User [bold]{user}[/] has been successfully logged out.");
        }
    }
}
