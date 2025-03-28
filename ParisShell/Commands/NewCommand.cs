using ParisShell.Services;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParisShell.Commands
{
    internal class NewCommand : ICommand
    {
        public string Name => "newc";
        private readonly SqlService _sqlService;
        private readonly Session _session;

        public NewCommand(SqlService sqlService, Session session) {
            _sqlService = sqlService;
            _session = session;
        }

        public void Execute(string[] args)
        {
            if (!_session.IsAuthenticated)
            {
                Shell.PrintError("You must be logged to order");
                return;
            }

            if (!_session.IsInRole("CLIENT"))
            {
                AnsiConsole.MarkupLine("You must be a client to order");
                return;
            }
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[green]Bienvenue dans l'espace de commande ![/]");
            AnsiConsole.MarkupLine("[blue]Voici la liste des commandes disponibles :[/]");
        }
    }
}
