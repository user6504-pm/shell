using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParisShell.Commands
{

    /// <summary>
    /// Command that clears the console screen using Spectre.Console.
    /// </summary>
    internal class ClearCommand : ICommand
    {

        /// <summary>
        /// Name of the command used to trigger it from the shell.
        /// </summary>
        public string Name => "clear";

        /// <summary>
        /// Executes the clear command by wiping the terminal screen.
        /// </summary>
        public void Execute(string[] args)
        {
            AnsiConsole.Clear();
        }
    }
}
