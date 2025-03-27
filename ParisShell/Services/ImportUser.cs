using MySql.Data.MySqlClient;
using OfficeOpenXml;
using System;
using System.IO;

namespace ParisShell.Services {
    internal class ImportUser {
        public static void ImportUtilisateursMySql(string cheminExcel, MySqlConnection connexion) {
            FileInfo fichier = new FileInfo(cheminExcel);
            using ExcelPackage package = new ExcelPackage(fichier);

            var feuilleUsers = package.Workbook.Worksheets["users"];
            var feuilleRoles = package.Workbook.Worksheets["user_roles"];
            var feuilleClients = package.Workbook.Worksheets["clients"];

            var rolesDeBase = new[] { "CLIENT", "CUISINIER", "ADMIN", "BOZO" };

            foreach (var role in rolesDeBase) {
                string requeteInsertRole = @"
        INSERT INTO roles (role_name)
        SELECT @name
        FROM DUAL
        WHERE NOT EXISTS (SELECT 1 FROM roles WHERE role_name = @name);";

                using var cmdRole = new MySqlCommand(requeteInsertRole, connexion);
                cmdRole.Parameters.AddWithValue("@name", role);
                cmdRole.ExecuteNonQuery();
            }


            int nbLignesUsers = feuilleUsers.Dimension.Rows;
            for (int ligne = 2; ligne <= nbLignesUsers; ligne++) {
                string requeteUser = @"
                    INSERT INTO users (user_id, nom, prenom, adresse, telephone, email, mdp, metroproche)
                    VALUES (@id, @nom, @prenom, @adresse, @tel, @email, @mdp, @metro);";

                using MySqlCommand cmd = new MySqlCommand(requeteUser, connexion);
                cmd.Parameters.AddWithValue("@id", feuilleUsers.Cells[ligne, 1].GetValue<int>());
                cmd.Parameters.AddWithValue("@nom", feuilleUsers.Cells[ligne, 2].Text);
                cmd.Parameters.AddWithValue("@prenom", feuilleUsers.Cells[ligne, 3].Text);
                cmd.Parameters.AddWithValue("@adresse", feuilleUsers.Cells[ligne, 4].Text);
                cmd.Parameters.AddWithValue("@tel", feuilleUsers.Cells[ligne, 5].Text);
                cmd.Parameters.AddWithValue("@email", feuilleUsers.Cells[ligne, 6].Text);
                cmd.Parameters.AddWithValue("@mdp", feuilleUsers.Cells[ligne, 7].Text);
                cmd.Parameters.AddWithValue("@metro", feuilleUsers.Cells[ligne, 8].GetValue<int>());
                cmd.ExecuteNonQuery();
            }

            int nbLignesRoles = feuilleRoles.Dimension.Rows;
            for (int ligne = 2; ligne <= nbLignesRoles; ligne++) {
                string requeteRole = @"
                    INSERT INTO user_roles (user_id, role_id)
                    VALUES (@userId, (SELECT role_id FROM roles WHERE role_name = @roleName));";

                using MySqlCommand cmd = new MySqlCommand(requeteRole, connexion);
                cmd.Parameters.AddWithValue("@userId", feuilleRoles.Cells[ligne, 1].GetValue<int>());
                cmd.Parameters.AddWithValue("@roleName", feuilleRoles.Cells[ligne, 2].Text);
                cmd.ExecuteNonQuery();
            }

            int nbLignesClients = feuilleClients.Dimension.Rows;
            for (int ligne = 2; ligne <= nbLignesClients; ligne++) {
                string requeteClient = @"
                    INSERT INTO clients (client_id, type_client)
                    VALUES (@id, @type);";

                using MySqlCommand cmd = new MySqlCommand(requeteClient, connexion);
                cmd.Parameters.AddWithValue("@id", feuilleClients.Cells[ligne, 1].GetValue<int>());
                cmd.Parameters.AddWithValue("@type", feuilleClients.Cells[ligne, 2].Text);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
