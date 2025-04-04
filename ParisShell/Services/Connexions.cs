using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;

namespace ParisShell.Services {
    /// <summary>
    /// Provides functionality to read metro connection data from an Excel file 
    /// and insert the connections into a MySQL database.
    /// </summary>
    internal class Connexions {
        private static HashSet<string> insertedConnections = new HashSet<string>();

        /// <summary>
        /// Reads metro connection data from an Excel file and inserts it into the database.
        /// </summary>
        /// <param name="excelPath">Path to the Excel file containing station and connection data.</param>
        /// <param name="connection">An open MySQL connection to the target database.</param>
        public static void ConnexionsSql(string excelPath, MySqlConnection connection) {
            FileInfo file = new FileInfo(excelPath);
            ExcelPackage package = new ExcelPackage(file);

            ExcelWorksheet connectionsSheet = package.Workbook.Worksheets[1];
            ExcelWorksheet stationsSheet = package.Workbook.Worksheets[0];
            int rowCount = connectionsSheet.Dimension.End.Row;

            for (int row = 2; row <= rowCount; row++) {
                string stationIdText = connectionsSheet.Cells[row, 1].Text;

                bool isEmpty = string.IsNullOrWhiteSpace(stationIdText);
                if (!isEmpty) {
                    int stationId = Convert.ToInt32(stationIdText);

                    int previousId = -1;
                    string previousText = connectionsSheet.Cells[row, 3].Text;
                    if (!string.IsNullOrWhiteSpace(previousText) && previousText != "0")
                        previousId = Convert.ToInt32(previousText.Trim());

                    int nextId = -1;
                    string nextText = connectionsSheet.Cells[row, 4].Text;
                    if (!string.IsNullOrWhiteSpace(nextText) && nextText != "0")
                        nextId = Convert.ToInt32(nextText.Trim());

                    if (previousId != -1) {
                        InsererConnexion(connection, stationsSheet, stationId, previousId);
                    }

                    if (nextId != -1) {
                        InsererConnexion(connection, stationsSheet, stationId, nextId);
                    }
                }
            }

            package.Dispose();
        }

        /// <summary>
        /// Inserts a connection between two stations into the database if it hasn't been added already.
        /// Calculates the geographic distance using the Haversine formula.
        /// </summary>
        /// <param name="connection">MySQL database connection.</param>
        /// <param name="sheet">Worksheet containing station location data.</param>
        /// <param name="id1">ID of the first station.</param>
        /// <param name="id2">ID of the second station.</param>
        private static void InsererConnexion(MySqlConnection connection, ExcelWorksheet sheet, int id1, int id2) {
            string key = $"{id1}-{id2}";
            bool alreadyExists = insertedConnections.Contains(key);

            if (!alreadyExists) {
                insertedConnections.Add(key);

                int row1 = TrouverLigne(sheet, id1);
                int row2 = TrouverLigne(sheet, id2);

                double lat1 = Convert.ToDouble(sheet.Cells[row1, 5].Text, CultureInfo.InvariantCulture) * Math.PI / 180;
                double lon1 = Convert.ToDouble(sheet.Cells[row1, 4].Text, CultureInfo.InvariantCulture) * Math.PI / 180;
                double lat2 = Convert.ToDouble(sheet.Cells[row2, 5].Text, CultureInfo.InvariantCulture) * Math.PI / 180;
                double lon2 = Convert.ToDouble(sheet.Cells[row2, 4].Text, CultureInfo.InvariantCulture) * Math.PI / 180;

                double dlat = lat2 - lat1;
                double dlon = lon2 - lon1;

                double a = Math.Pow(Math.Sin(dlat / 2), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dlon / 2), 2);
                double c = 2 * Math.Asin(Math.Sqrt(a));
                double distance = 6371.0 * c * 1000; // distance in meters

                using (MySqlCommand cmd = new MySqlCommand(
                    "INSERT INTO connexions_metro (station1_id, station2_id, distance_m) VALUES (@id1, @id2, @dist)", connection)) {
                    cmd.Parameters.AddWithValue("@id1", id1);
                    cmd.Parameters.AddWithValue("@id2", id2);
                    cmd.Parameters.AddWithValue("@dist", distance);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Finds the row number of a given station ID in the Excel worksheet.
        /// </summary>
        /// <param name="sheet">The worksheet containing station data.</param>
        /// <param name="stationId">The station ID to find.</param>
        /// <returns>The row number where the station is found.</returns>
        /// <exception cref="Exception">Thrown if the station ID is not found.</exception>
        private static int TrouverLigne(ExcelWorksheet sheet, int stationId) {
            int rowCount = sheet.Dimension.End.Row;
            for (int row = 2; row <= rowCount; row++) {
                string idText = sheet.Cells[row, 1].Text.Trim();
                if (int.TryParse(idText, out int id) && id == stationId)
                    return row;
            }

            throw new Exception($"Station ID {stationId} not found in the Excel file.");
        }
    }
}
