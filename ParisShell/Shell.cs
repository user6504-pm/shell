using ParisShell.Commands;
using ParisShell.Services;
using Spectre.Console;
using System;
using System.Collections.Generic;
using ZstdSharp.Unsafe;

namespace ParisShell {
    internal class Shell {
        private readonly Dictionary<string, Action<string[]>> commands = new();
        private readonly SqlService _sqlService = new SqlService();
        private readonly Session _session = new();

        public Shell() {
            commands["clear"] = args => new ClearCommand().Execute(args);
            commands["connect"] = args => new ConnectCommand(_sqlService).Execute(args);
            commands["disconnect"] = args => new DisconnectCommand(_sqlService, _session).Execute(args);
            commands["showtable"] = args => new ShowTableCommand(_sqlService, _session).Execute(args);
            commands["showtables"] = args => new ShowTablesCommand(_sqlService, _session).Execute(args);
            commands["cinf"] = args => new CInfCommand(_sqlService, _session).Execute(args);
            commands["login"] = args => new LoginCommand(_sqlService, _session).Execute(args);
            commands["initdb"] = args => new InitDbCommand().Execute(args);
            commands["autoconnect"] = args => new AutoConnectCommand(_sqlService, _session).Execute(args);
            commands["user"] = args => new UserCommand(_sqlService, _session).Execute(args);
            commands["cook"] = args => new CuisinierCommand(_sqlService, _session).Execute(args);
            commands["analytics"] = args => new AnalyticsCommand(_sqlService, _session).Execute(args);
            commands["logout"] = args => new LogoutCommand(_session).Execute(args);
            commands["tuto"] = args => new TutoCommand().Execute(args);
            commands["graph"] = args => new GraphCommand(_sqlService).Execute(args);
            commands["help"] = args => new HelpCommand(_sqlService, _session).Execute(args);
            commands["register"] = args => new RegisterCommand(_sqlService).Execute(args);
            commands["edit"] = args => new EditCommand(_sqlService, _session).Execute(args);
            commands["client"] = args => new ClientCommand(_sqlService, _session).Execute(args);
            commands["deleteacc"] = args => new DeleteAccCommand(_sqlService, _session).Execute(args);
        }

        public void Run() {
            AnsiConsole.Clear();

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
                ? _session.CurrentUser.FirstName
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
