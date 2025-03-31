using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParisShell.Commands
{
    internal class AddQCommand : ICommand
    {
        public string Name => "addq";

        public void Execute(string[] args)
        {
            AnsiConsole.Clear();
        }
    }
}
