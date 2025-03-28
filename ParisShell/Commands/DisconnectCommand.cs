using Spectre.Console;
using ParisShell.Services;

namespace ParisShell.Commands {
    internal class DisconnectCommand : ICommand {

        private readonly SqlService _sqlService;
        private readonly Session _session;

        public string Name => "disconnect";

        public DisconnectCommand(SqlService sqlService, Session _sesssion) {
            _sqlService = sqlService;
            _session = _sesssion;
        }

        public void Execute(string[] args) {
            if (!_sqlService.IsConnected) {
                Shell.PrintWarning("Not connected: nothing to disconnect from.");
                return;
            }

            _sqlService.Disconnect();
            Shell.PrintSucces("Disconnected from MySQL.");

            // Clear user session
            if (_session.CurrentUser != null) {
                string user = _session.CurrentUser.Email;
                _session.CurrentUser = null;
                Shell.PrintSucces($"User [bold]{user}[/] logged out.");
            }
        }
    }
}
