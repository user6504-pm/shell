using ParisShell.Commands;
using Spectre.Console;
using System;
using System.Collections.Generic;

namespace ParisShell {
    internal class Shell {
        private readonly Dictionary<string, Action<string[]>> commands = new();

        public Shell() {
            commands["pwd"] = args => Pwd();
            commands["echo"] = args => Echo(args);
            commands["update"] = args => SimulateUpdate();
            commands["help"] = args => Help();
            commands["connect"] = args => new ConnectCommand().Execute(args);
            commands["disconnect"] = args => new DisconnectCommand().Execute(args);
        }

        public void Run() {

            while (true) {

                var promptText = $"[white]user[/][deeppink4_2]@paris[/][maroon]:{Statusus()}[/][white]#[/] ";

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

        private void Pwd() {
            AnsiConsole.MarkupLine($"[blue]{Environment.CurrentDirectory}[/]");
        }

        private void Echo(string[] args) {
            var text = string.Join(" ", args);
            AnsiConsole.MarkupLine(text);
        }

        private void SimulateUpdate() {
            AnsiConsole.MarkupLine("[white]Password for user:[/]");
            AnsiConsole.MarkupLine("[olive]Hit:1[/] [white]http://archive.ubuntu.com/ubuntu jammy InRelease[/]");
            AnsiConsole.MarkupLine("[olive]Hit:2[/] [white]http://security.ubuntu.com/ubuntu jammy-security InRelease[/]");
            AnsiConsole.MarkupLine("[olive]Hit:3[/] [white]http://archive.ubuntu.com/ubuntu jammy-updates InRelease[/]");

            AnsiConsole.Write(new Markup("[green]0%[/] [bold yellow]Working[/]\n"));
            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .Start("Updating package lists...", ctx => {
                    System.Threading.Thread.Sleep(2000);
                });

            AnsiConsole.MarkupLine("[green]Done.[/]");
        }

        private void Help() {
            AnsiConsole.MarkupLine("[bold]Commandes disponibles :[/]");
            AnsiConsole.MarkupLine("[blue]pwd[/]        - Affiche le répertoire courant");
            AnsiConsole.MarkupLine("[blue]echo[/]       - Affiche du texte");
            AnsiConsole.MarkupLine("[blue]update[/]     - Simule une mise à jour apt");
            AnsiConsole.MarkupLine("[blue]exit[/]       - Quitte le shell");
        }

        private void PrintError(string message) {
            AnsiConsole.MarkupLine($"[maroon]Erreur :[/] {message}");
        }
        private string Statusus() {
            return "";
        }
    }
}
