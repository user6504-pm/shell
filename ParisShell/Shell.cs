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
            commands["showtable"] = args => new ShowTableCommand(_sqlService).Execute(args);
            commands["showtables"] = args => new ShowTablesCommand(_sqlService).Execute(args);
            commands["cinf"] = args => new CInfCommand(_sqlService).Execute(args);
            commands["login"] = args => new LoginCommand(_sqlService, _session).Execute(args);
            commands["initdb"] = args => new InitDbCommand().Execute(args);
        }

        public void Run() {

            while (true) {

                var promptText = $"[white]user[/][deeppink4_2]@paris[/][maroon]:{Statusus()}[/][white]#[/] ";
                string userDisplay = _session.CurrentUser != null
                                    ? $"[white]{_session.CurrentUser.Nom}[/][deeppink4_2]@paris[/][maroon]"
                                    : $"[white]user[/][deeppink4_2]@paris[/][maroon]:{Statusus()}[/][white]#[/] ";

                var input = AnsiConsole.Prompt(
                    new TextPrompt<string>(promptText)
                        .PromptStyle("white")
                        .AllowEmpty());



                if (string.IsNullOrWhiteSpace(input))
                    continue;

                if (input.Trim().ToLower() == "exit") {
                    AnsiConsole.MarkupLine("[white]Session terminée.[/]");
                    break;
                }

                string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string name = parts[0];
                string[] args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

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

        private void PrintError(string message) {
            AnsiConsole.MarkupLine($"[maroon]Erreur :[/] {message}");
        }
        private string Statusus() {
            return "";
        }
    }
}
