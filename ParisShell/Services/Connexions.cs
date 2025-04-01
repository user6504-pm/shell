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
    internal class Connexions {
        private static HashSet<string> connexionsInserees = new HashSet<string>();

        public static void ConnexionsSql(string cheminExcel, MySqlConnection maConnexion) {
            FileInfo fichier = new FileInfo(cheminExcel);
            ExcelPackage package = new ExcelPackage(fichier);

            ExcelWorksheet feuilleConnexions = package.Workbook.Worksheets[1];
            ExcelWorksheet feuilleStations = package.Workbook.Worksheets[0];
            int nbLignes = feuilleConnexions.Dimension.End.Row;

            for (int ligne = 2; ligne <= nbLignes; ligne++) {
                string idStationText = feuilleConnexions.Cells[ligne, 1].Text;
                if (string.IsNullOrWhiteSpace(idStationText)) continue;

                int idStation = Convert.ToInt32(idStationText);

                int idPrecedent = -1;
                string textPrecedent = feuilleConnexions.Cells[ligne, 3].Text;
                if (!string.IsNullOrWhiteSpace(textPrecedent) && textPrecedent != "0")
                    idPrecedent = Convert.ToInt32(textPrecedent.Trim());

                int idSuivant = -1;
                string textSuivant = feuilleConnexions.Cells[ligne, 4].Text;
                if (!string.IsNullOrWhiteSpace(textSuivant) && textSuivant != "0")
                    idSuivant = Convert.ToInt32(textSuivant.Trim());

                if (idPrecedent != -1) {
                    InsererConnexion(maConnexion, feuilleStations, idStation, idPrecedent);
                }

                if (idSuivant != -1) {
                    InsererConnexion(maConnexion, feuilleStations, idStation, idSuivant);
                }
            }

            package.Dispose();
        }

        private static void InsererConnexion(MySqlConnection connexion, ExcelWorksheet feuille, int id1, int id2) {

            string cle = $"{id1}-{id2}";
            if (connexionsInserees.Contains(cle))
                return;

            connexionsInserees.Add(cle);

            int ligne1 = TrouverLigne(feuille, id1);
            int ligne2 = TrouverLigne(feuille, id2);

            double lat1 = Convert.ToDouble(feuille.Cells[ligne1, 5].Text, CultureInfo.InvariantCulture) * Math.PI / 180;
            double lon1 = Convert.ToDouble(feuille.Cells[ligne1, 4].Text, CultureInfo.InvariantCulture) * Math.PI / 180;
            double lat2 = Convert.ToDouble(feuille.Cells[ligne2, 5].Text, CultureInfo.InvariantCulture) * Math.PI / 180;
            double lon2 = Convert.ToDouble(feuille.Cells[ligne2, 4].Text, CultureInfo.InvariantCulture) * Math.PI / 180;

            double dlat = lat2 - lat1;
            double dlon = lon2 - lon1;


            double a = Math.Pow(Math.Sin(dlat / 2), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dlon / 2), 2);
            double c = 2 * Math.Asin(Math.Sqrt(a));
            double distance = 6371.0 * c * 1000;


            using (MySqlCommand cmd = new MySqlCommand(
                "INSERT INTO connexions_metro (station1_id, station2_id, distance_m) VALUES (@id1, @id2, @dist)", connexion)) {
                cmd.Parameters.AddWithValue("@id1", id1);
                cmd.Parameters.AddWithValue("@id2", id2);
                cmd.Parameters.AddWithValue("@dist", distance);
                cmd.ExecuteNonQuery();
            }
        }
        private static int TrouverLigne(ExcelWorksheet feuille, int stationId) {
            int nbLignes = feuille.Dimension.End.Row;
            for (int ligne = 2; ligne <= nbLignes; ligne++) {
                string idText = feuille.Cells[ligne, 1].Text.Trim();
                if (int.TryParse(idText, out int id) && id == stationId)
                    return ligne;
            }

            throw new Exception($"Station ID {stationId} non trouvée dans le fichier Excel.");
        }
    }
}
