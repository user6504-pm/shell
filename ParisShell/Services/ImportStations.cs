using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;

namespace ParisShell.Services {
    internal class ImportStations {
        public static void ImportStationsMySql(string cheminExcel, MySqlConnection maConnexion) {
            FileInfo fichier = new FileInfo(cheminExcel);
            HashSet<int> stationIdsVus = new HashSet<int>();

            using (ExcelPackage package = new ExcelPackage(fichier)) {
                ExcelWorksheet feuille = package.Workbook.Worksheets[0];
                int nbLignes = feuille.Dimension.Rows;

                for (int ligne = 2; ligne <= nbLignes; ligne++) {
                    string idText = feuille.Cells[ligne, 1].Text.Trim();

                    if (!int.TryParse(idText, out int stationId)) continue;

                    if (stationIdsVus.Contains(stationId)) continue;

                    stationIdsVus.Add(stationId);

                    string ligneMetro = feuille.Cells[ligne, 2].Text.Trim();
                    string nomStation = feuille.Cells[ligne, 3].Text.Trim();
                    string longitudeText = feuille.Cells[ligne, 4].Text.Trim().Replace(".", ",");
                    string latitudeText = feuille.Cells[ligne, 5].Text.Trim().Replace(".", ",");
                    string commune = feuille.Cells[ligne, 6].Text.Trim();
                    string inseeText = feuille.Cells[ligne, 7].Text.Trim();

                    if (!double.TryParse(longitudeText, out double longitude) ||
                        !double.TryParse(latitudeText, out double latitude) ||
                        !int.TryParse(inseeText, out int insee)) continue;

                    string requete = @"INSERT INTO stations_metro 
                (libelle_ligne, libelle_station, longitude, latitude, commune, insee) 
                VALUES (@ligne, @station, @long, @lat, @commune, @insee);";

                    using (MySqlCommand cmd = new MySqlCommand(requete, maConnexion)) {
                        cmd.Parameters.AddWithValue("@ligne", ligneMetro);
                        cmd.Parameters.AddWithValue("@station", nomStation);
                        cmd.Parameters.AddWithValue("@long", longitude);
                        cmd.Parameters.AddWithValue("@lat", latitude);
                        cmd.Parameters.AddWithValue("@commune", commune);
                        cmd.Parameters.AddWithValue("@insee", insee);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
