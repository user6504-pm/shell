using Spectre.Console;
using ParisShell.Services;

namespace ParisShell.Commands {
    internal class DisconnectCommand : ICommand {

        private readonly SqlService _sqlService;

        public string Name => "disconnect";

        public DisconnectCommand(SqlService sqlService) {
            _sqlService = sqlService;
        }

        public void Execute(string[] args) {
            _sqlService.Disconnect();
        }
    }
}
