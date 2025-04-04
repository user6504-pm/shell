using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using OfficeOpenXml;
using MySql.Data.MySqlClient;

/// <summary>
/// Handles the import of dish data from an Excel file into a MySQL database.
/// </summary>
public class ImportDishes {
    /// <summary>
    /// Imports dish records from an Excel file and inserts them into the "plats" table in the database.
    /// Each cook (with the role "CUISINIER") is randomly assigned between 1 and 3 dishes.
    /// </summary>
    /// <param name="excelPath">Path to the Excel file containing dish data.</param>
    /// <param name="connection">An open MySQL database connection.</param>
    public static void ImportDishesSQL(string excelPath, MySqlConnection connection) {
        FileInfo file = new FileInfo(excelPath);
        ExcelPackage package = new ExcelPackage(file);

        ExcelWorksheet sheet = package.Workbook.Worksheets[0];
        int rowCount = sheet.Dimension.End.Row;

        List<int> cooks = new List<int>();
        MySqlCommand cmd = new MySqlCommand(@"
            SELECT ur.user_id FROM user_roles ur
            JOIN roles r ON ur.role_id = r.role_id
            WHERE r.role_name = 'CUISINIER';", connection);

        MySqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read()) {
            cooks.Add(reader.GetInt32(0));
        }
        reader.Close();
        cmd.Dispose();

        Random rand = new Random();
        int row = 2;
        int cookIndex = 0;

        while (row <= rowCount && cookIndex < cooks.Count) {
            int cookId = cooks[cookIndex];
            int dishCount = rand.Next(1, 4);
            cookIndex++;

            for (int i = 0; i < dishCount && row <= rowCount; i++, row++) {
                try {
                    string dishName = sheet.Cells[row, 11].Text;
                    string dishType = sheet.Cells[row, 2].Text;
                    int peopleCount = int.Parse(sheet.Cells[row, 3].Text);
                    decimal price = decimal.Parse(sheet.Cells[row, 6].Text, new CultureInfo("fr-FR"));
                    int quantity = rand.Next(1, 7);
                    DateTime productionDate = sheet.Cells[row, 4].GetValue<DateTime>();
                    DateTime expiryDate = sheet.Cells[row, 5].GetValue<DateTime>();
                    string nationality = sheet.Cells[row, 7].Text;
                    string diet = sheet.Cells[row, 8].Text;
                    string ingredients = sheet.Cells[row, 9].Text;
                    string photo = sheet.Cells[row, 10].Text;

                    string query = @"INSERT INTO plats 
                        (user_id, plat_name, type_plat, nb_personnes, quantite, date_fabrication, date_peremption, 
                        prix_par_personne, nationalite, regime_alimentaire, ingredients, photo)
                        VALUES (@uid, @dishName, @type, @people, @quantity, @prod, @exp, @price, @nation, @diet, @ingredients, @photo);";

                    MySqlCommand insertCmd = new MySqlCommand(query, connection);
                    insertCmd.Parameters.AddWithValue("@uid", cookId);
                    insertCmd.Parameters.AddWithValue("@dishName", dishName);
                    insertCmd.Parameters.AddWithValue("@type", dishType);
                    insertCmd.Parameters.AddWithValue("@people", peopleCount);
                    insertCmd.Parameters.AddWithValue("@quantity", quantity);
                    insertCmd.Parameters.AddWithValue("@prod", productionDate);
                    insertCmd.Parameters.AddWithValue("@exp", expiryDate);
                    insertCmd.Parameters.AddWithValue("@price", price);
                    insertCmd.Parameters.AddWithValue("@nation", nationality);
                    insertCmd.Parameters.AddWithValue("@diet", diet);
                    insertCmd.Parameters.AddWithValue("@ingredients", ingredients);
                    insertCmd.Parameters.AddWithValue("@photo", photo);
                    insertCmd.ExecuteNonQuery();
                    insertCmd.Dispose();
                }
                catch (Exception ex) {
                    Console.WriteLine($" ERROR line {row} : {ex.Message}");
                }
            }
        }

        package.Dispose();
    }
}
