using Spectre.Console;
using ParisShell.Services;
using MySql.Data.MySqlClient;
using System.Data;
using OfficeOpenXml;

namespace ParisShell.Commands {
    internal class InitDbCommand : ICommand {
        public string Name => "initdb";

        public void Execute(string[] args) {
            AnsiConsole.MarkupLine("[yellow]⚠️ Initialisation de la base de données Livininparis_219...[/]");

            string mdp = AnsiConsole.Prompt(
                new TextPrompt<string>("Mot de passe MySQL [grey](root)[/]:")
                .PromptStyle("red")
                .Secret());

            string cheminExcel = "../../../../MetroParis.xlsx";
            FileInfo fichierExcel = new FileInfo(cheminExcel);

            MySqlConnection maConnexion = null;
            try {
                string connexionString = "SERVER=localhost;PORT=3306;" +
                                         "DATABASE=sys;" +
                                         "UID=root;PASSWORD=" + mdp;

                maConnexion = new MySqlConnection(connexionString);
                maConnexion.Open();
                AnsiConsole.MarkupLine("[green]✅ Connexion à MySQL réussie.[/]");
            }
            catch (MySqlException e) {
                AnsiConsole.MarkupLine($"[red]⛔ Erreur de connexion :[/] {e.Message}");
                return;
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
                @"CREATE TABLE IF NOT EXISTS cuisiniers (
                    cuisinier_id INT PRIMARY KEY,
                    FOREIGN KEY (cuisinier_id) REFERENCES users(user_id) ON UPDATE CASCADE ON DELETE CASCADE
                );",
                @"CREATE TABLE IF NOT EXISTS plats (
                    plat_id INT AUTO_INCREMENT PRIMARY KEY,
                    cuisinier_id INT NOT NULL,
                    type_plat ENUM('ENTREE','PLAT PRINCIPAL','DESSERT') NOT NULL,
                    nb_personnes INT NOT NULL,
                    date_fabrication DATE NOT NULL,
                    date_peremption DATE NOT NULL,
                    prix_par_personne DECIMAL(6,2) NOT NULL,
                    nationalite VARCHAR(50),
                    regime_alimentaire VARCHAR(100),
                    ingredients TEXT,
                    photo VARCHAR(255),
                    FOREIGN KEY (cuisinier_id) REFERENCES cuisiniers(cuisinier_id) ON UPDATE CASCADE ON DELETE CASCADE
                );",
                @"CREATE TABLE IF NOT EXISTS commandes (
                    commande_id INT AUTO_INCREMENT PRIMARY KEY,
                    client_id INT NOT NULL,
                    plat_id INT NOT NULL,
                    date_commande DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    quantite INT NOT NULL,
                    statut ENUM('EN_COURS','LIVREE','ANNULEE') DEFAULT 'EN_COURS',
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

            foreach (var query in tableQueries) {
                try {
                    using var command = maConnexion.CreateCommand();
                    command.CommandText = query;
                    command.ExecuteNonQuery();
                }
                catch (MySqlException e) {
                    AnsiConsole.MarkupLine($"[red]⛔ Erreur SQL :[/] {e.Message}");
                    return;
                }
            }

            AnsiConsole.MarkupLine("[green]✅ Tables créées avec succès.[/]");

            try {
                AnsiConsole.MarkupLine("[blue]📥 Importation des stations...[/]");
                ImportStations.ImportStationsMySql(cheminExcel, maConnexion);

                AnsiConsole.MarkupLine("[blue]🔗 Importation des connexions métro...[/]");
                Connexions.ConnexionsSql(cheminExcel, maConnexion);

                AnsiConsole.MarkupLine("[green]✅ Données importées avec succès.[/]");
            }
            catch (Exception ex) {
                AnsiConsole.MarkupLine($"[red]⛔ Erreur d'import :[/] {ex.Message}");
            }

            maConnexion.Close();
            AnsiConsole.MarkupLine("[grey]🔌 Connexion fermée.[/]");
        }
    }
}
