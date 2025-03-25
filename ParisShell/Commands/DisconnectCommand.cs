using Spectre.Console;
using ParisShell.Services;

namespace ParisShell.Commands {
    internal class DisconnectCommand : ICommand {
        public string Name => "disconnect";

        public void Execute(string[] args) {
            var sqlService = new SqlService();

            if (sqlService.IsConnected) {
                sqlService.Disconnect();
                AnsiConsole.MarkupLine("[green]✅ Déconnexion réussie de la base de données.[/]");
            }
            else {
                AnsiConsole.MarkupLine("[yellow]⚠️ Aucune connexion active à déconnecter.[/]");
            }
        }
    }
}
