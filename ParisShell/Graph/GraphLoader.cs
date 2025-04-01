using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using Spectre.Console;
using ParisShell.Services;
using System.Data;
using OfficeOpenXml;
using ParisShell.Models;

namespace ParisShell.Graph {
    internal class GraphLoader {
        public static void ConstruireEtAfficherGraph(MySqlConnection connexion, string nomFichier = "graphe_metro.svg") {
            Graph<StationData> graphe = new Graph<StationData>();
            Dictionary<int, Noeud<StationData>> noeudsDict = new Dictionary<int, Noeud<StationData>>();


            MySqlCommand cmdStations = new MySqlCommand("SELECT station_id, libelle_station, longitude, latitude FROM stations_metro", connexion);
            MySqlDataReader readerStations = cmdStations.ExecuteReader();

            while (readerStations.Read()) {
                int id = readerStations.GetInt32(0);
                string nom = readerStations.GetString(1);
                double longitude = readerStations.GetDouble(2);
                double latitude = readerStations.GetDouble(3);

                StationData data = new StationData {
                    Nom = nom,
                    Longitude = longitude,
                    Latitude = latitude
                };

                var noeud = new Noeud<StationData>(id, data);
                graphe.AjouterNoeud(noeud);
                noeudsDict[id] = noeud;

            }

            readerStations.Close();
            cmdStations.Dispose();

            MySqlCommand cmdConnexions = new MySqlCommand("SELECT station1_id, station2_id, distance_m FROM connexions_metro", connexion);
            MySqlDataReader readerConnexions = cmdConnexions.ExecuteReader();

            while (readerConnexions.Read()) {
                int id1 = readerConnexions.GetInt32(0);
                int id2 = readerConnexions.GetInt32(1);
                int distance = Convert.ToInt32(readerConnexions.GetDouble(2));

                if (noeudsDict.TryGetValue(id1, out var noeud1) && noeudsDict.TryGetValue(id2, out var noeud2)) {
                    graphe.AjouterLien(noeud1, noeud2, distance);
                }
            }

            readerConnexions.Close();
            cmdConnexions.Dispose();

            AnsiConsole.Status()
                .Start("Creating graph...", ctx => {
                    try {
                        ctx.Spinner(Spinner.Known.Flip);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        int idDepart = 101;
                        int idArrivee = 217;

                        if (!noeudsDict.TryGetValue(idDepart, out var noeudDepart)) {
                            Shell.PrintError("Station de départ introuvable.");
                            return;
                        }

                        if (!noeudsDict.TryGetValue(idArrivee, out var noeudArrivee)) {
                            Shell.PrintError("Station d'arrivée introuvable.");
                            return;
                        }

                        var chemin = graphe.BellmanFordCheminPlusCourt(noeudDepart, noeudArrivee);
                        graphe.ExporterSvg(nomFichier);
                        Shell.PrintSucces("Graph created successfully.");
                    }
                    catch (Exception ex) {
                        Shell.PrintError($"Import error: {ex.Message}");
                    }
                });
        }
    }
}
