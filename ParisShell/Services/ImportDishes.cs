﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using OfficeOpenXml;
using MySql.Data.MySqlClient;

public class ImportDishes
{
    public static void ImportDishesSQL(string cheminExcel, MySqlConnection maConnexion)
    {
        FileInfo fichier = new FileInfo(cheminExcel);
        ExcelPackage package = new ExcelPackage(fichier);

        ExcelWorksheet feuille = package.Workbook.Worksheets[0];
        int nbLignes = feuille.Dimension.End.Row;
        List<int> cuisiniers = new List<int>();
        MySqlCommand cmd = new MySqlCommand(@"
            SELECT ur.user_id FROM user_roles ur
            JOIN roles r ON ur.role_id = r.role_id
            WHERE r.role_name = 'CUISINIER';", maConnexion);

        MySqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            cuisiniers.Add(reader.GetInt32(0));
        }
        reader.Close();
        cmd.Dispose();


        Random rand = new Random();
        int ligne = 2;
        int indexCuisinier = 0;
        while (ligne <= nbLignes && indexCuisinier < cuisiniers.Count)
        {
            int cuisinierId = cuisiniers[indexCuisinier];
            int nbPlats = rand.Next(1, 4); 
            indexCuisinier++;

            for (int i = 0; i < nbPlats && ligne <= nbLignes; i++, ligne++)
            {
                try
                {
                    string platName = feuille.Cells[ligne, 11].Text;
                    string typePlat = feuille.Cells[ligne, 2].Text;
                    int nbPersonnes = int.Parse(feuille.Cells[ligne, 3].Text);
                    decimal prix = decimal.Parse(feuille.Cells[ligne, 6].Text, new CultureInfo("fr-FR"));
                    int quantite = rand.Next(1, 7);
                    DateTime fabrication = feuille.Cells[ligne, 4].GetValue<DateTime>();
                    DateTime peremption = feuille.Cells[ligne, 5].GetValue<DateTime>();
                    string nationalite = feuille.Cells[ligne, 7].Text;
                    string regime = feuille.Cells[ligne, 8].Text;
                    string ingredients = feuille.Cells[ligne, 9].Text;
                    string photo = feuille.Cells[ligne, 10].Text;

                    string query = @"INSERT INTO plats 
                (user_id, plat_name, type_plat, nb_personnes,quantite, date_fabrication, date_peremption, 
                prix_par_personne, nationalite, regime_alimentaire, ingredients, photo)
                VALUES (@uid,@platName, @type, @nb,@quantite, @fab, @per, @prix, @nat, @regime, @ing, @photo);";

                    MySqlCommand insertCmd = new MySqlCommand(query, maConnexion);
                    insertCmd.Parameters.AddWithValue("@uid", cuisinierId);
                    insertCmd.Parameters.AddWithValue("@platName", platName);
                    insertCmd.Parameters.AddWithValue("@type", typePlat);
                    insertCmd.Parameters.AddWithValue("@nb", nbPersonnes);
                    insertCmd.Parameters.AddWithValue("@quantite", quantite);
                    insertCmd.Parameters.AddWithValue("@fab", fabrication);
                    insertCmd.Parameters.AddWithValue("@per", peremption);
                    insertCmd.Parameters.AddWithValue("@prix", prix);
                    insertCmd.Parameters.AddWithValue("@nat", nationalite);
                    insertCmd.Parameters.AddWithValue("@regime", regime);
                    insertCmd.Parameters.AddWithValue("@ing", ingredients);
                    insertCmd.Parameters.AddWithValue("@photo", photo);
                    insertCmd.ExecuteNonQuery();
                    insertCmd.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($" ERROR line {ligne} : {ex.Message}");
                }
            }
        }


        package.Dispose();
    }
}
