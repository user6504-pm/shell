using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParisShell.Commands {
    internal class LsCommand : ICommand {
        public string Name => "ls";

        public void Execute(string[] args) {
            string path = Directory.GetCurrentDirectory();
            string[] entries = Directory.GetFileSystemEntries(path);
            foreach (string entry in entries) {
                Console.WriteLine(Path.GetFileName(entry));
            }
        }
    }
}
