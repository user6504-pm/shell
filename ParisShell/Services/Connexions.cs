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
        public static void ConnexionsSql(string cheminExcel, MySqlConnection maConnexion) {
            FileInfo fichier = new FileInfo(cheminExcel);

            ExcelPackage package = new ExcelPackage(fichier);
            ExcelWorksheet feuille = package.Workbook.Worksheets[1];
            ExcelWorksheet feuille0 = package.Workbook.Worksheets[0];
            int nbLignes = feuille.Dimension.End.Row;

            for (int ligne = 2; ligne <= nbLignes; ligne++) {
                string idStationText = feuille.Cells[ligne, 1].Text;
                int idStation = Convert.ToInt32(idStationText);

                int idPrecedent = -1;
                string textPrecedent = feuille.Cells[ligne, 3].Text;

                if (!string.IsNullOrWhiteSpace(textPrecedent) && textPrecedent != "0") {
                    idPrecedent = Convert.ToInt32(textPrecedent.Trim());
                }
                int idSuivant = -1;
                string textSuivant = feuille.Cells[ligne, 4].Text;

                if (!string.IsNullOrWhiteSpace(textSuivant) && textSuivant != "0") {
                    idSuivant = Convert.ToInt32(textSuivant.Trim());
                }
                if (idPrecedent != -1) {
                    double lat1 = Convert.ToDouble(feuille0.Cells[ligne, 5].Text, CultureInfo.InvariantCulture) * Math.PI / 180;
                    double lon1 = Convert.ToDouble(feuille0.Cells[ligne, 4].Text, CultureInfo.InvariantCulture) * Math.PI / 180;

                    double lat2 = Convert.ToDouble(feuille0.Cells[idPrecedent + 1, 5].Text, CultureInfo.InvariantCulture) * Math.PI / 180;
                    double lon2 = Convert.ToDouble(feuille0.Cells[idPrecedent + 1, 4].Text, CultureInfo.InvariantCulture) * Math.PI / 180;

                    double dlat = lat2 - lat1;
                    double dlon = lon2 - lon1;

                    double a = Math.Pow(Math.Sin(dlat / 2), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dlon / 2), 2);
                    double c = 2 * Math.Asin(Math.Sqrt(a));
                    double R = 6371.0;
                    double distance = R * c;

                    MySqlCommand cmd = new MySqlCommand("INSERT INTO connexions_metro (station1_id, station2_id, distance_m) VALUES (@id1, @id2, @dist)", maConnexion);
                    cmd.Parameters.AddWithValue("@id1", idStation);
                    cmd.Parameters.AddWithValue("@id2", idPrecedent);
                    cmd.Parameters.AddWithValue("@dist", distance * 1000);
                    cmd.ExecuteNonQuery();
                    cmd.Dispose();
                }

                if (idSuivant != -1) {
                    double lat1 = Convert.ToDouble(feuille0.Cells[ligne, 5].Text, CultureInfo.InvariantCulture) * Math.PI / 180;
                    double lon1 = Convert.ToDouble(feuille0.Cells[ligne, 4].Text, CultureInfo.InvariantCulture) * Math.PI / 180;

                    double lat2 = Convert.ToDouble(feuille0.Cells[idSuivant + 1, 5].Text, CultureInfo.InvariantCulture) * Math.PI / 180;
                    double lon2 = Convert.ToDouble(feuille0.Cells[idSuivant + 1, 4].Text, CultureInfo.InvariantCulture) * Math.PI / 180;

                    double dlat = lat2 - lat1;
                    double dlon = lon2 - lon1;

                    double a = Math.Pow(Math.Sin(dlat / 2), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dlon / 2), 2);
                    double c = 2 * Math.Asin(Math.Sqrt(a));
                    double R = 6371.0;
                    double distance = R * c;

                    MySqlCommand cmd = new MySqlCommand("INSERT INTO connexions_metro (station1_id, station2_id, distance_m) VALUES (@id1, @id2, @dist)", maConnexion);
                    cmd.Parameters.AddWithValue("@id1", idStation);
                    cmd.Parameters.AddWithValue("@id2", idSuivant);
                    cmd.Parameters.AddWithValue("@dist", distance * 1000);
                    cmd.ExecuteNonQuery();
                    cmd.Dispose();
                }
            }

            package.Dispose();
        }


    }
}
