using System;
using System.Collections.Generic;
using System.IO;
using ParisShell.Commands;

namespace ParisShell {
    class Program {
        static void Main(string[] args) {
            var shell = new Shell();
            shell.Run();
        }
    }
}
