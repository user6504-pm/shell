using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;

namespace ParisShell.Services
{
    internal class ImportStations
    {
        public static void ImportStationsMySql(string excelPath, MySqlConnection connection)
        {
            FileInfo file = new FileInfo(excelPath);
            HashSet<int> seenStationIds = new HashSet<int>();

            using (ExcelPackage package = new ExcelPackage(file))
            {
                ExcelWorksheet sheet = package.Workbook.Worksheets[0];
                int rowCount = sheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++)
                {
                    string idText = sheet.Cells[row, 1].Text.Trim();

                    if (!int.TryParse(idText, out int stationId)) continue;
                    if (seenStationIds.Contains(stationId)) continue;

                    seenStationIds.Add(stationId);

                    string subwayLine = sheet.Cells[row, 2].Text.Trim();
                    string stationName = sheet.Cells[row, 3].Text.Trim();
                    string longitudeText = sheet.Cells[row, 4].Text.Trim().Replace(".", ",");
                    string latitudeText = sheet.Cells[row, 5].Text.Trim().Replace(".", ",");
                    string municipality = sheet.Cells[row, 6].Text.Trim();
                    string inseeText = sheet.Cells[row, 7].Text.Trim();

                    if (!double.TryParse(longitudeText, out double longitude) ||
                        !double.TryParse(latitudeText, out double latitude) ||
                        !int.TryParse(inseeText, out int insee)) continue;

                    string query = @"INSERT INTO stations_metro 
                (libelle_ligne, libelle_station, longitude, latitude, commune, insee) 
                VALUES (@line, @station, @long, @lat, @municipality, @insee);";

                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@line", subwayLine);
                        cmd.Parameters.AddWithValue("@station", stationName);
                        cmd.Parameters.AddWithValue("@long", longitude);
                        cmd.Parameters.AddWithValue("@lat", latitude);
                        cmd.Parameters.AddWithValue("@municipality", municipality);
                        cmd.Parameters.AddWithValue("@insee", insee);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
