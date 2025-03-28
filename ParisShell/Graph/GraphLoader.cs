using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using Spectre.Console;
using ParisShell.Services;
using System.Data;
using OfficeOpenXml;

namespace ParisShell.Graph
{
    internal class GraphLoader
    {
        public static void ConstruireEtAfficherGraph(MySqlConnection connexion, string nomFichier = "graphe_metro.png")
        {
            Graph<string> graphe = new Graph<string>();
            Dictionary<int, Noeud<string>> noeudsDict = new Dictionary<int, Noeud<string>>();

            MySqlCommand cmdStations = new MySqlCommand("SELECT station_id, libelle_station FROM stations_metro", connexion);
            MySqlDataReader readerStations = cmdStations.ExecuteReader();

            while (readerStations.Read())
            {
                int id = readerStations.GetInt32(0);
                string nom = readerStations.GetString(1);

                Noeud<string> noeud = new Noeud<string>(id, nom);
                graphe.AjouterNoeud(noeud);
                noeudsDict[id] = noeud;
            }

            readerStations.Close();
            cmdStations.Dispose();

            MySqlCommand cmdConnexions = new MySqlCommand("SELECT station1_id, station2_id, distance_m FROM connexions_metro", connexion);
            MySqlDataReader readerConnexions = cmdConnexions.ExecuteReader();

            while (readerConnexions.Read())
            {
                int id1 = readerConnexions.GetInt32(0);
                int id2 = readerConnexions.GetInt32(1);
                int distance = Convert.ToInt32(readerConnexions.GetDouble(2));

                if (noeudsDict.ContainsKey(id1) && noeudsDict.ContainsKey(id2))
                {
                    Noeud<string> noeud1 = noeudsDict[id1];
                    Noeud<string> noeud2 = noeudsDict[id2];
                    graphe.AjouterLien(noeud1, noeud2, distance);
                }
            }

            readerConnexions.Close();
            cmdConnexions.Dispose();
            AnsiConsole.Status()
                .Start("Creating graph...", ctx => {
                    try
                    {
                        ctx.Spinner(Spinner.Known.Dots4);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        graphe.AfficherGraphique(nomFichier, 1200);
                        Shell.PrintSucces("Graph created successfully.");
                    }
                    catch (Exception ex)
                    {
                        Shell.PrintError($"Import error: {ex.Message}");
                    }
                });

        }
    }
}
