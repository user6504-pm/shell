using System;
using System.Data;
using MySql.Data.MySqlClient;
using Spectre.Console;
using ParisShell.Models;

namespace ParisShell.Services
{
    internal class SqlService
    {
        private MySqlConnection? _connection;

        public MySqlConnection GetConnection() => _connection;
        public bool IsConnected => _connection?.State == ConnectionState.Open;

        public bool Connect(SqlConnectionConfig config)
        {
            if (!config.IsValid())
            {
                Shell.PrintError("Invalid connection parameters.");
                return false;
            }

            string connStr = $"SERVER={config.SERVER};PORT={config.PORT};" +
                             $"DATABASE={config.DATABASE};" +
                             $"UID={config.UID};PASSWORD={config.PASSWORD}";

            try
            {
                _connection = new MySqlConnection(connStr);
                _connection.Open();

                Shell.PrintSucces($"Successfully connected to [bold]{config.DATABASE}[/].");
                return true;
            }
            catch (Exception ex)
            {
                Shell.PrintError("Connection failed: " + ex.Message);
                return false;
            }
        }

        public void Disconnect()
        {
            if (_connection?.State == ConnectionState.Open)
            {
                _connection.Close();
                Shell.PrintWarning("Disconnected from database.");
            }
        }

        public void ExecuteAndDisplay(string sql)
        {
            if (!IsConnected)
            {
                Shell.PrintError("Not connected to a database.");
                return;
            }

            try
            {
                using var cmd = new MySqlCommand(sql, _connection);
                using var reader = cmd.ExecuteReader();

                if (!reader.HasRows)
                {
                    Shell.PrintWarning("No data returned.");
                    return;
                }

                var table = new Table().Border(TableBorder.Rounded);

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    table.AddColumn($"[bold]{reader.GetName(i)}[/]");
                }

                while (reader.Read())
                {
                    var row = new List<string>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row.Add(reader[i]?.ToString() ?? "");
                    }
                    table.AddRow(row.ToArray());
                }

                AnsiConsole.Write(table);
            }
            catch (Exception ex)
            {
                Shell.PrintError("SQL Error: " + ex.Message);
            }
        }
    }
}
