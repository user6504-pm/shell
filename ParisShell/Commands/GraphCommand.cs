using ParisShell.Graph;
using ParisShell.Services;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParisShell.Commands
{
    internal class GraphCommand : ICommand
    {
        public string Name => "graph";
        private readonly SqlService _sqlService;

        public GraphCommand(SqlService sqlService)
        {
            _sqlService = sqlService;
        }
        public void Execute(string[] args)
        {
            GraphLoader.ConstruireEtAfficherGraph(_sqlService.GetConnection());
        }
    }
}
