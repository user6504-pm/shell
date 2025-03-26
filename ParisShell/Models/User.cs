using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParisShell.Models {
    internal class User {
        public int Id { get; set; }
        public string Nom { get; set; } = "";
        public string Prenom { get; set; } = "";
        public string Email { get; set; } = "";
        public List<string> Roles { get; set; } = new();
    }

}
