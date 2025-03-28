﻿using ParisShell.Commands;
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
            commands["logout"] = args => new LogoutCommand(_session).Execute(args);
            commands["tuto"] = args => new TutoCommand().Execute(args);
            commands["graph"] = args => new GraphCommand(_sqlService).Execute(args);
            commands["help"] = args => new HelpCommand(_session).Execute(args);

        }

        public void Run() {

            AnsiConsole.MarkupLine(@"
[white] ____    ______  ____    ______  ____                             [/]
[white]/\  _`\ /\  _  \/\  _`\ /\__  _\/\  _`\                            [/]
[white]\ \ \L\ \ \ \L\ \ \ \L\ \/_/\ \/\ \,\L\_\                          [/]
[white] \ \ ,__/\ \  __ \ \ ,  /  \ \ \ \/_\__ \                          [/]
[white]  \ \ \/  \ \ \/\ \ \ \\ \  \_\ \__/\ \L\ \                        [/]
[white]   \ \_\   \ \_\ \_\ \_\ \_\/\_____\ `\____\                       [/]
[white]    \/_/    \/_/\/_/\/_/\/ /\/_____/\/_____/                       [/]
                                                                        
[deeppink4_2]                       ____    __  __  ____    __       __        [/]
[deeppink4_2]                      /\  _`\ /\ \/\ \/\  _`\ /\ \     /\ \       [/]
[deeppink4_2]                      \ \,\L\_\ \ \_\ \ \ \L\_\ \ \    \ \ \      [/]
[deeppink4_2]                       \/_\__ \\ \  _  \ \  _\L\ \ \  __\ \ \  __ [/]
[deeppink4_2]                         /\ \L\ \ \ \ \ \ \ \L\ \ \ \L\ \\ \ \L\ \[/]
[deeppink4_2]                         \ `\____\ \_\ \_\ \____/\ \____/ \ \____/[/]
[deeppink4_2]                          \/_____/\/_/\/_/\/___/  \/___/   \/___/ [/]
");
            AnsiConsole.MarkupLine("[dim]ParisShell v1.0[/]");
            AnsiConsole.MarkupLine("[dim]Use 'tuto' for getting started[/]\n");

            while (true) {
                var prompt = $"[white]{GetPromptUser()}[/][deeppink4_2]@paris[/][white]:{Statusus()}[/][white]$[/] ";

                var input = AnsiConsole.Prompt(
                    new TextPrompt<string>(prompt)
                        .PromptStyle("white")
                        .AllowEmpty());

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string name = parts[0].ToLower();
                string[] args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

                var autoriséesSansConnexion = new HashSet<string> { "tuto", "connect", "exit", "help", "clear", "initdb", "autoconnect" };
                if (!_sqlService.IsConnected && !autoriséesSansConnexion.Contains(name)) {
                    PrintError("[maroon]not connected to any mysql server.[/]");
                    continue;
                }

                var autoriséesSansLogin = new HashSet<string> { "login", "exit", "help", "clear", "connect", "initdb", "disconnect", "graph"};
                if (_sqlService.IsConnected && !_session.IsAuthenticated && !autoriséesSansLogin.Contains(name)) {
                    AnsiConsole.MarkupLine("[maroon]not logged in any acc[/]");
                    continue;
                }

                if (name == "exit") {
                    PrintSucces("[white]Session ended.[/]");
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
                    PrintError($"[white]Unknown command : '{name}'[/]");
                }
            }
        }

        private string GetPromptUser() {
            return _session.IsAuthenticated
                ? _session.CurrentUser.Nom
                : _sqlService.IsConnected ? "mysql" : "anon";
        }

        public static void PrintError(string message) {
            AnsiConsole.MarkupLine($"[bold red]ERROR[/][bold white]: [/]{message}");
        }
        public static void PrintSucces(string message) {
            AnsiConsole.MarkupLine($"[bold lime]SUCCES[/][bold white]: [/]{message}");
        }
        public static void PrintWarning(string message) {
            AnsiConsole.MarkupLine($"[bold orange1]WARNING[/][bold white]: [/]{message}");
        }

        private string Statusus() {
            if (!_sqlService.IsConnected)
                return "[red]mysql:~[/]";
            if (!_session.IsAuthenticated)
                return "[orange1]auth:~[/]";
            return "[green]~[/]";
        }
    }
}
