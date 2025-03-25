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

            using (ExcelPackage package = new ExcelPackage(fichier)) {
                ExcelWorksheet feuille = package.Workbook.Worksheets[0];
                int nbLignes = feuille.Dimension.Rows;

                for (int ligne = 2; ligne <= nbLignes; ligne++) {
                    string ligneMetro = feuille.Cells[ligne, 2].Text;
                    string nomStation = feuille.Cells[ligne, 3].Text;
                    string longitude = feuille.Cells[ligne, 4].Text.Replace(".", ",");
                    string latitude = feuille.Cells[ligne, 5].Text.Replace(".", ",");
                    string commune = feuille.Cells[ligne, 6].Text;
                    string insee = feuille.Cells[ligne, 7].Text;

                    string requete = @"INSERT INTO stations_metro 
                    (libelle_ligne, libelle_station, longitude, latitude, commune, insee) 
                    VALUES (@ligne, @station, @long, @lat, @commune, @insee);";

                    using (MySqlCommand cmd = new MySqlCommand(requete, maConnexion)) {
                        cmd.Parameters.AddWithValue("@ligne", ligneMetro);
                        cmd.Parameters.AddWithValue("@station", nomStation);
                        cmd.Parameters.AddWithValue("@long", Convert.ToDouble(longitude));
                        cmd.Parameters.AddWithValue("@lat", Convert.ToDouble(latitude));
                        cmd.Parameters.AddWithValue("@commune", commune);
                        cmd.Parameters.AddWithValue("@insee", Convert.ToInt32(insee));
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
