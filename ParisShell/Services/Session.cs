using ParisShell.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParisShell.Services {
    internal class Session {
        public User? CurrentUser { get; set; }
        public bool IsAuthenticated => CurrentUser != null;
        public bool IsInRole(string role) => CurrentUser?.Roles.Contains(role) ?? false;
    }

}
