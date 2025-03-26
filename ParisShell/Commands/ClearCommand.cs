using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParisShell.Commands {
    internal class ClearCommand : ICommand {
        public string Name => "clear";

        public void Execute(string[] args) {
            AnsiConsole.Clear();
        }
    }
}
