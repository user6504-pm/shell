using ParisShell.Services;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace ParisShell.Commands
{
    internal class NewCCommand : ICommand
    {
        public string Name => "newc";
        private readonly SqlService _sqlService;
        private readonly Session _session;

        public NewCCommand(SqlService sqlService, Session session)
        {
            _sqlService = sqlService;
            _session = session;
        }

        public void Execute(string[] args)
        {
            AnsiConsole.Clear();

            if (!_sqlService.IsConnected)
            {
                Shell.PrintError("Vous devez être connecté à la base de données.");
                return;
            }

            if (!_session.IsAuthenticated || !_session.IsInRole("CLIENT"))
            {
                Shell.PrintError("❌ Seuls les clients peuvent passer une commande.");
                return;
            }
        }
    }
}
