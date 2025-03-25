namespace ParisShell.Models {
    internal class SqlConnectionConfig {
        public string SERVER { get; set; } = "localhost";
        public string PORT { get; set; } = "3306";
        public string DATABASE { get; set; } = "sys";
        public string UID { get; set; } = "root";
        public string PASSWORD { get; set; } = "root";

        public bool IsValid() {
            return !string.IsNullOrWhiteSpace(SERVER)
                && !string.IsNullOrWhiteSpace(PORT)
                && !string.IsNullOrWhiteSpace(DATABASE)
                && !string.IsNullOrWhiteSpace(UID)
                && !string.IsNullOrWhiteSpace(PASSWORD);
        }
    }
}
