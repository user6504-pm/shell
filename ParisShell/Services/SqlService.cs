﻿using System;
using System.Data;
using MySql.Data.MySqlClient;
using Spectre.Console;
using ParisShell.Models;

namespace ParisShell.Services {
    /// <summary>
    /// Provides methods for managing MySQL database connections and executing SQL queries.
    /// </summary>
    internal class SqlService {
        private MySqlConnection? _connection;

        /// <summary>
        /// Gets the current MySQL connection instance.
        /// </summary>
        /// <returns>The current <see cref="MySqlConnection"/> object.</returns>
        public MySqlConnection GetConnection() => _connection;

        /// <summary>
        /// Gets a value indicating whether the service is connected to the database.
        /// </summary>
        public bool IsConnected => _connection?.State == ConnectionState.Open;

        /// <summary>
        /// Establishes a connection to the MySQL database using the provided configuration.
        /// </summary>
        /// <param name="config">An instance of <see cref="SqlConnectionConfig"/> containing connection parameters.</param>
        /// <returns>True if the connection is successful; otherwise, false.</returns>
        public bool Connect(SqlConnectionConfig config) {
            if (!config.IsValid()) {
                Shell.PrintError("Invalid connection parameters.");
                return false;
            }

            string connStr = $"SERVER={config.SERVER};PORT={config.PORT};" +
                             $"DATABASE={config.DATABASE};" +
                             $"UID={config.UID};PASSWORD={config.PASSWORD}";

            try {
                _connection = new MySqlConnection(connStr);
                _connection.Open();

                Shell.PrintSucces($"Successfully connected to [bold]{config.DATABASE}[/].");
                return true;
            }
            catch (Exception ex) {
                Shell.PrintError("Connection failed: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Closes the database connection if it is currently open.
        /// </summary>
        public void Disconnect() {
            if (_connection?.State == ConnectionState.Open) {
                _connection.Close();
                Shell.PrintWarning("Disconnected from database.");
            }
        }

        /// <summary>
        /// Executes a SQL query and displays the results in a formatted table using Spectre.Console.
        /// </summary>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="parameters">Optional dictionary of query parameters.</param>
        public void ExecuteAndDisplay(string sql, Dictionary<string, object> parameters = null) {
            try {
                using var cmd = new MySqlCommand(sql, _connection);
                if (parameters != null) {
                    foreach (var param in parameters)
                        cmd.Parameters.AddWithValue(param.Key, param.Value);
                }

                using var reader = cmd.ExecuteReader();

                if (!IsConnected) {
                    Shell.PrintError("Not connected to a database.");
                    return;
                }

                if (!reader.HasRows) {
                    Shell.PrintWarning("No data returned.");
                    return;
                }

                var table = new Table().Border(TableBorder.Rounded);

                for (int i = 0; i < reader.FieldCount; i++) {
                    table.AddColumn($"[bold]{reader.GetName(i)}[/]");
                }

                while (reader.Read()) {
                    var row = new List<string>();
                    for (int i = 0; i < reader.FieldCount; i++) {
                        row.Add(reader[i]?.ToString() ?? "");
                    }
                    table.AddRow(row.ToArray());
                }

                AnsiConsole.Write(table);
            }
            catch (Exception ex) {
                Shell.PrintError("SQL Error: " + ex.Message);
            }
        }
    }
}
