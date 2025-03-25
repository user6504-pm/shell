using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParisShell.Commands {
    internal class PwdCommand : ICommand {
        public string Name => "help";

        public void Execute(string[] args) {
            Console.WriteLine(Directory.GetCurrentDirectory());
        }
    }
}
