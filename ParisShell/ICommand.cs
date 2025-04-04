using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParisShell {
    /// <summary>
    /// Represents a command that can be executed in the shell.
    /// </summary>
    internal interface ICommand {
        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Executes the command with the specified arguments.
        /// </summary>
        /// <param name="args">An array of arguments passed to the command.</param>
        void Execute(string[] args);
    }
}
