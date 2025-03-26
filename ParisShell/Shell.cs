using ParisShell.Commands;
using ParisShell.Services;
using Spectre.Console;
using System;
using System.Collections.Generic;

namespace ParisShell {
    internal class Shell {
        private readonly Dictionary<string, Action<string[]>> commands = new();
        private readonly SqlService _sqlService = new SqlService();
        private readonly Session _session = new();

        public Shell() {
            commands["clear"] = args => new ClearCommand().Execute(args);
            commands["connect"] = args => new ConnectCommand(_sqlService).Execute(args);
            commands["disconnect"] = args => new DisconnectCommand(_sqlService).Execute(args);
            commands["showtable"] = args => new ShowTableCommand(_sqlService, _session).Execute(args);
            commands["showtables"] = args => new ShowTablesCommand(_sqlService, _session).Execute(args);
            commands["cinf"] = args => new CInfCommand(_sqlService, _session).Execute(args);
            commands["login"] = args => new LoginCommand(_sqlService, _session).Execute(args);
            commands["initdb"] = args => new InitDbCommand().Execute(args);
            commands["autoconnect"] = args => new AutoConnectCommand(_sqlService, _session).Execute(args);
            commands["user"] = args => new UserCommand(_sqlService, _session).Execute(args);
            commands["cuisinier"] = args => new CuisinierCommand(_sqlService, _session).Execute(args);
            commands["analytics"] = args => new AnalyticsCommand(_sqlService, _session).Execute(args);

        }

        public void Run() {
            AnsiConsole.MarkupLine("[grey]ParisShell v1.0[/]");
            AnsiConsole.MarkupLine("[dim]Utilisez 'connect' pour vous connecter à MySQL, puis 'login'.[/]\n");

            while (true) {
                var prompt = $"[white]{GetPromptUser()}[/][deeppink4_2]@paris[/][maroon]:{Statusus()}[/][white]#[/] ";

                var input = AnsiConsole.Prompt(
                    new TextPrompt<string>(prompt)
                        .PromptStyle("white")
                        .AllowEmpty());

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string name = parts[0].ToLower();
                string[] args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

                // Étape 1 : autoriser uniquement certaines commandes si MySQL NON connecté
                var autoriséesSansConnexion = new HashSet<string> { "connect", "exit", "help", "clear", "initdb", "autoconnect" };
                if (!_sqlService.IsConnected && !autoriséesSansConnexion.Contains(name)) {
                    AnsiConsole.MarkupLine("[red]⛔ MySQL non connecté. Utilisez 'connect' pour vous connecter.[/]");
                    continue;
                }

                // Étape 2 : autoriser uniquement login une fois MySQL OK mais pas encore d'utilisateur
                var autoriséesSansLogin = new HashSet<string> { "login", "exit", "help", "clear", "connect", "initdb" };
                if (_sqlService.IsConnected && !_session.IsAuthenticated && !autoriséesSansLogin.Contains(name)) {
                    AnsiConsole.MarkupLine("[red]🔐 Veuillez d'abord vous authentifier avec 'login'.[/]");
                    continue;
                }

                if (name == "exit") {
                    AnsiConsole.MarkupLine("[white]Session terminée.[/]");
                    break;
                }

                if (commands.TryGetValue(name, out var action)) {
                    try {
                        action.Invoke(args);
                    }
                    catch (Exception ex) {
                        PrintError(ex.Message);
                    }
                }
                else {
                    PrintError($"Commande inconnue : '{name}'");
                }
            }
        }

        private string GetPromptUser() {
            return _session.IsAuthenticated
                ? _session.CurrentUser.Nom
                : _sqlService.IsConnected ? "mysql" : "anon";
        }

        private void PrintError(string message) {
            AnsiConsole.MarkupLine($"[maroon]Erreur :[/] {message}");
        }

        private string Statusus() {
            if (!_sqlService.IsConnected)
                return "[red]mysql:X[/]";
            if (!_session.IsAuthenticated)
                return "[orange1]auth:X[/]";
            return "[green]✓[/]";
        }
    }
}
