using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using ParisShell.Commands;

namespace ParisShell {
    /// <summary>
    /// Entry point of the ParisShell application.
    /// Initializes and starts the interactive shell.
    /// </summary>
    class Program {
        /// <summary>
        /// Main method that starts the shell application.
        /// </summary>
        /// <param name="args">Command-line arguments passed to the application.</param>
        static void Main(string[] args) {
            var shell = new Shell();
            shell.Run();
        }
    }
}
