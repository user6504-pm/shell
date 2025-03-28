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

        public void Execute(string[] args)
        {
            AnsiConsole.Clear();
        }
    }
}
