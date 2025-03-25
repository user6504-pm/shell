using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParisShell.Commands {
    internal class EchoCommand : ICommand {
        public string Name => "echo";

        public void Execute(string[] args) {
            Console.WriteLine(string.Join(" ", args));
        }
    }
}
