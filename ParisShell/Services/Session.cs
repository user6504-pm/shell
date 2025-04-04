using ParisShell.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParisShell.Services {
    /// <summary>
    /// Manages the current session state, including the authenticated user and role-based access.
    /// </summary>
    internal class Session {
        /// <summary>
        /// Gets or sets the currently authenticated user.
        /// </summary>
        public User? CurrentUser { get; set; }

        /// <summary>
        /// Gets a value indicating whether a user is currently authenticated.
        /// </summary>
        public bool IsAuthenticated => CurrentUser != null;

        /// <summary>
        /// Checks whether the authenticated user has a specific role.
        /// </summary>
        /// <param name="role">The role name to check.</param>
        /// <returns>True if the user has the role; otherwise, false.</returns>
        public bool IsInRole(string role) => CurrentUser?.Roles.Contains(role) ?? false;
    }
}
