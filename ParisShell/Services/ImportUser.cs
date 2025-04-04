using MySql.Data.MySqlClient;
using OfficeOpenXml;
using System;
using System.IO;

namespace ParisShell.Services
{
    internal class ImportUser
    {
        public static void ImportUtilisateursMySql(string excelPath, MySqlConnection myConnection)
        {
            FileInfo file = new FileInfo(excelPath);
            using ExcelPackage package = new ExcelPackage(file);

            var usersSheet = package.Workbook.Worksheets["users"];
            var rolesSheet = package.Workbook.Worksheets["user_roles"];
            var clientsSheet = package.Workbook.Worksheets["clients"];

            var baseRoles = new[] { "CLIENT", "CUISINIER", "ADMIN", "BOZO" };

            foreach (var role in baseRoles)
            {
                string insertRoleQuery = @"
        INSERT INTO roles (role_name)
        SELECT @name
        FROM DUAL
        WHERE NOT EXISTS (SELECT 1 FROM roles WHERE role_name = @name);";

                using var roleCmd = new MySqlCommand(insertRoleQuery, myConnection);
                roleCmd.Parameters.AddWithValue("@name", role);
                roleCmd.ExecuteNonQuery();
            }

            int userRowCount = usersSheet.Dimension.Rows;
            for (int row = 2; row <= userRowCount; row++)
            {
                string insertUserQuery = @"
                    INSERT INTO users (user_id, nom, prenom, adresse, telephone, email, mdp, metroproche)
                    VALUES (@id, @lastName, @firstName, @address, @phone, @email, @password, @nearestStation);";

                using MySqlCommand cmd = new MySqlCommand(insertUserQuery, myConnection);
                cmd.Parameters.AddWithValue("@id", usersSheet.Cells[row, 1].GetValue<int>());
                cmd.Parameters.AddWithValue("@lastName", usersSheet.Cells[row, 2].Text);
                cmd.Parameters.AddWithValue("@firstName", usersSheet.Cells[row, 3].Text);
                cmd.Parameters.AddWithValue("@address", usersSheet.Cells[row, 4].Text);
                cmd.Parameters.AddWithValue("@phone", usersSheet.Cells[row, 5].Text);
                cmd.Parameters.AddWithValue("@email", usersSheet.Cells[row, 6].Text);
                cmd.Parameters.AddWithValue("@password", usersSheet.Cells[row, 7].Text);
                cmd.Parameters.AddWithValue("@nearestStation", usersSheet.Cells[row, 8].GetValue<int>());
                cmd.ExecuteNonQuery();
            }

            int roleRowCount = rolesSheet.Dimension.Rows;
            for (int row = 2; row <= roleRowCount; row++)
            {
                string insertUserRoleQuery = @"
                    INSERT INTO user_roles (user_id, role_id)
                    VALUES (@userId, (SELECT role_id FROM roles WHERE role_name = @roleName));";

                using MySqlCommand cmd = new MySqlCommand(insertUserRoleQuery, myConnection);
                cmd.Parameters.AddWithValue("@userId", rolesSheet.Cells[row, 1].GetValue<int>());
                cmd.Parameters.AddWithValue("@roleName", rolesSheet.Cells[row, 2].Text);
                cmd.ExecuteNonQuery();
            }

            int clientRowCount = clientsSheet.Dimension.Rows;
            for (int row = 2; row <= clientRowCount; row++)
            {
                string insertClientQuery = @"
                    INSERT INTO clients (client_id, type_client)
                    VALUES (@id, @type);";

                using MySqlCommand cmd = new MySqlCommand(insertClientQuery, myConnection);
                cmd.Parameters.AddWithValue("@id", clientsSheet.Cells[row, 1].GetValue<int>());
                cmd.Parameters.AddWithValue("@type", clientsSheet.Cells[row, 2].Text);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
