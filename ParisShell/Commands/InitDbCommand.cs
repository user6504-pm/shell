﻿using Spectre.Console;
using ParisShell.Services;
using MySql.Data.MySqlClient;
using System.Data;
using OfficeOpenXml;

namespace ParisShell.Commands
{

    /// <summary>
    /// Command to initialize the MySQL database for the application.
    /// It drops the old database, recreates all necessary tables, and imports data from Excel files.
    /// </summary>
    internal class InitDbCommand : ICommand
    {

        /// <summary>
        /// Name of the command as used in the CLI.
        /// </summary>
        public string Name => "initdb";

        /// <summary>
        /// Executes the database initialization process:
        /// 1. Connects to MySQL using a password prompt.
        /// 2. Drops and recreates the database and tables.
        /// 3. Imports station, user, connection, and dish data from Excel.
        /// </summary>
        /// <param name="args">Optional command-line arguments (not used here).</param>
        public void Execute(string[] args)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            Shell.PrintWarning("Initializing database [bold]Livininparis_219[/]...");

            Console.CursorVisible = false;

            string pwd = AnsiConsole.Prompt(
                new TextPrompt<string>("MySQL password [grey](root)[/]:")
                .PromptStyle("red")
                .Secret(' '));

            string ExcelPath = "../../../../Infos_Excel/MetroParis.xlsx";
            FileInfo ExcelFile = new FileInfo(ExcelPath);

            MySqlConnection MyConnection = null;

            try
            {
                string ConnectionString = "SERVER=localhost;PORT=3306;" +
                                          "DATABASE=sys;" +
                                          "UID=root;PASSWORD=" + pwd;

                MyConnection = new MySqlConnection(ConnectionString);
                MyConnection.Open();
                Shell.PrintSucces("Connected to MySQL.");
            }
            catch (MySqlException e)
            {
                Shell.PrintError($"Connection error: {e.Message}");
                return;
            }
            finally
            {
                Console.CursorVisible = true;
            }

            List<string> tableQueries = new()
            {
                @"DROP DATABASE IF EXISTS Livininparis_219;",
                @"CREATE DATABASE Livininparis_219;",
                @"USE Livininparis_219;",
                @"CREATE TABLE IF NOT EXISTS roles (
                    role_id INT AUTO_INCREMENT PRIMARY KEY,
                    role_name VARCHAR(50) NOT NULL
                );",
                @"CREATE TABLE IF NOT EXISTS stations_metro (
                    station_id INT AUTO_INCREMENT PRIMARY KEY,
                    libelle_ligne VARCHAR(100) NOT NULL,
                    libelle_station VARCHAR(100) NOT NULL,
                    longitude DOUBLE NOT NULL,
                    latitude DOUBLE NOT NULL,
                    commune VARCHAR(100) NOT NULL,
                    insee INT NOT NULL
                );",
                @"CREATE TABLE IF NOT EXISTS users (
                    user_id INT AUTO_INCREMENT PRIMARY KEY,
                    nom VARCHAR(100) NOT NULL,
                    prenom VARCHAR(100),
                    adresse VARCHAR(250) NOT NULL,
                    telephone VARCHAR(20),
                    email VARCHAR(100) NOT NULL UNIQUE,
                    mdp VARCHAR(255) NOT NULL,
                    metroproche INT,
                    FOREIGN KEY (metroproche) REFERENCES stations_metro(station_id) ON DELETE CASCADE
                );",
                @"CREATE TABLE IF NOT EXISTS user_roles (
                    user_id INT NOT NULL,
                    role_id INT NOT NULL,
                    PRIMARY KEY (user_id, role_id),
                    FOREIGN KEY (user_id) REFERENCES users(user_id) ON UPDATE CASCADE ON DELETE CASCADE,
                    FOREIGN KEY (role_id) REFERENCES roles(role_id) ON UPDATE CASCADE ON DELETE CASCADE
                );",
                @"CREATE TABLE IF NOT EXISTS clients (
                    client_id INT PRIMARY KEY,
                    type_client ENUM('PARTICULIER','ENTREPRISE') NOT NULL,
                    FOREIGN KEY (client_id) REFERENCES users(user_id) ON UPDATE CASCADE ON DELETE CASCADE
                );",
                @"CREATE TABLE IF NOT EXISTS plats (
                    plat_id INT AUTO_INCREMENT PRIMARY KEY,
                    user_id INT NOT NULL,
                    plat_name VARCHAR(100) NOT NULL,
                    type_plat ENUM('ENTREE','PLAT PRINCIPAL','DESSERT') NOT NULL,
                    nb_personnes INT NOT NULL,
                    quantite INT,
                    date_fabrication DATE NOT NULL,
                    date_peremption DATE NOT NULL,
                    prix_par_personne DECIMAL(6,2) NOT NULL,
                    nationalite VARCHAR(50),
                    regime_alimentaire VARCHAR(100),
                    ingredients TEXT,
                    photo VARCHAR(255),
                    FOREIGN KEY (user_id) REFERENCES users(user_id) ON UPDATE CASCADE ON DELETE CASCADE
                );",
                @"CREATE TABLE IF NOT EXISTS commandes (
                    commande_id INT AUTO_INCREMENT PRIMARY KEY,
                    client_id INT NOT NULL,
                    plat_id INT NOT NULL,
                    date_commande DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    quantite INT NOT NULL,
                    statut ENUM('EN_ATTENTE','EN_COURS_DE_LIVRAISON','LIVREE','ANNULEE','REFUSEE') DEFAULT 'EN_ATTENTE',
                    FOREIGN KEY (client_id) REFERENCES clients(client_id) ON UPDATE CASCADE ON DELETE CASCADE,
                    FOREIGN KEY (plat_id) REFERENCES plats(plat_id) ON UPDATE CASCADE ON DELETE CASCADE
                );",
                @"CREATE TABLE IF NOT EXISTS evaluations (
                    evaluation_id INT AUTO_INCREMENT PRIMARY KEY,
                    commande_id INT NOT NULL,
                    note INT NOT NULL CHECK (note BETWEEN 1 AND 5),
                    commentaire TEXT,
                    FOREIGN KEY (commande_id) REFERENCES commandes(commande_id)
                        ON UPDATE CASCADE ON DELETE CASCADE
                );",
                @"CREATE TABLE IF NOT EXISTS connexions_metro (
                    station1_id INT NOT NULL,
                    station2_id INT NOT NULL,
                    distance_m FLOAT(10,3) NOT NULL,
                    PRIMARY KEY (station1_id, station2_id),
                    FOREIGN KEY (station1_id) REFERENCES stations_metro(station_id)
                        ON UPDATE CASCADE ON DELETE CASCADE,
                    FOREIGN KEY (station2_id) REFERENCES stations_metro(station_id)
                        ON UPDATE CASCADE ON DELETE CASCADE
                );"
            };

            foreach (var query in tableQueries)
            {
                try
                {
                    using var command = MyConnection.CreateCommand();
                    command.CommandText = query;
                    command.ExecuteNonQuery();
                }
                catch (MySqlException e)
                {
                    Shell.PrintError($"SQL Error: {e.Message}");
                    return;
                }
            }

            Shell.PrintSucces("Tables successfully created.");

            AnsiConsole.Status()
                .Start("Importing data...", ctx => {
                    try
                    {
                        ctx.Spinner(Spinner.Known.Dots2);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        ctx.Status("Importing metro stations...");
                        ImportStations.ImportStationsMySql(ExcelPath, MyConnection);

                        ctx.Status("Importing metro connections...");
                        Connexions.ConnexionsSql(ExcelPath, MyConnection);

                        ctx.Status("Importing users...");
                        ImportUser.ImportUtilisateursMySql("../../../../Infos_Excel/user.xlsx", MyConnection);

                        ctx.Status("Importing dishes...");
                        ImportDishes.ImportDishesSQL("../../../../Infos_Excel/plats_simules.xlsx", MyConnection);

                        Shell.PrintSucces("Excel data imported successfully.");
                    }
                    catch (Exception ex)
                    {
                        Shell.PrintError($"Import error: {ex.Message}");
                    }
                });

            MyConnection.Close();
            Shell.PrintWarning("Connection closed.");
        }
    }
}
