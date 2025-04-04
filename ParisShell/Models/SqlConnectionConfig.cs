namespace ParisShell.Models {
    /// <summary>
    /// Represents the configuration settings required to establish a SQL connection.
    /// </summary>
    internal class SqlConnectionConfig {
        /// <summary>
        /// Gets or sets the server address where the SQL database is hosted.
        /// </summary>
        public string SERVER { get; set; } = "localhost";

        /// <summary>
        /// Gets or sets the port number used to connect to the SQL server.
        /// </summary>
        public string PORT { get; set; } = "3306";

        /// <summary>
        /// Gets or sets the name of the SQL database to connect to.
        /// </summary>
        public string DATABASE { get; set; } = "Livinparis_219";

        /// <summary>
        /// Gets or sets the username used to authenticate with the SQL database.
        /// </summary>
        public string UID { get; set; } = "root";

        /// <summary>
        /// Gets or sets the password used to authenticate with the SQL database.
        /// </summary>
        public string PASSWORD { get; set; } = "root";

        /// <summary>
        /// Validates whether all required configuration fields are properly set.
        /// </summary>
        /// <returns>
        /// Returns true if all fields are non-empty and non-whitespace; otherwise, false.
        /// </returns>
        public bool IsValid() {
            return !string.IsNullOrWhiteSpace(SERVER)
                && !string.IsNullOrWhiteSpace(PORT)
                && !string.IsNullOrWhiteSpace(DATABASE)
                && !string.IsNullOrWhiteSpace(UID)
                && !string.IsNullOrWhiteSpace(PASSWORD);
        }
    }
}
