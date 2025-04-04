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
        public static void ConstruireEtAfficherGraph(MySqlConnection connexion, int startId = 1, int endId = 43, string fileName = "graphe_metro.svg") {
            Graph<StationData> graph = new Graph<StationData>();
            Dictionary<int, Noeud<StationData>> nodesDict = new Dictionary<int, Noeud<StationData>>();


            MySqlCommand cmdStations = new MySqlCommand("SELECT station_id, libelle_station, longitude, latitude FROM stations_metro", connexion);
            MySqlDataReader readerStations = cmdStations.ExecuteReader();

            while (readerStations.Read()) {
                int id = readerStations.GetInt32(0);
                string name = readerStations.GetString(1);
                double longitude = readerStations.GetDouble(2);
                double latitude = readerStations.GetDouble(3);

                StationData data = new StationData {
                    Name = name,
                    Longitude = longitude,
                    Latitude = latitude
                };

                var noeud = new Noeud<StationData>(id, data);
                graph.AjouterNoeud(noeud);
                nodesDict[id] = noeud;

            }

            readerStations.Close();
            cmdStations.Dispose();

            MySqlCommand cmdConnexions = new MySqlCommand("SELECT station1_id, station2_id, distance_m FROM connexions_metro", connexion);
            MySqlDataReader readerConnexions = cmdConnexions.ExecuteReader();

            while (readerConnexions.Read()) {
                int id1 = readerConnexions.GetInt32(0);
                int id2 = readerConnexions.GetInt32(1);
                int length = Convert.ToInt32(readerConnexions.GetDouble(2));

                if (nodesDict.TryGetValue(id1, out var node1) && nodesDict.TryGetValue(id2, out var node2)) {
                    graph.AjouterLien(node1, node2, length);
                }
            }

            readerConnexions.Close();
            cmdConnexions.Dispose();

            AnsiConsole.Status()
                .Start("Creating graph...", ctx => {
                    try {
                        ctx.Spinner(Spinner.Known.Flip);
                        ctx.SpinnerStyle(Style.Parse("green"));

                        if (!nodesDict.TryGetValue(startId, out var nodeStart)) {
                            Shell.PrintError("Station de départ introuvable.");
                            return;
                        }

                        if (!nodesDict.TryGetValue(endId, out var nodeEnd)) {
                            Shell.PrintError("Station d'arrivée introuvable.");
                            return;
                        }

                        var chemin = graph.BellmanFordCheminPlusCourt(nodeStart, nodeEnd);
                        graph.ExporterSvg(fileName, chemin);
                        Shell.PrintSucces("Graph created successfully.");
                    }
                    catch (Exception ex) {
                        Shell.PrintError($"Import error: {ex.Message}");
                    }
                });
        }
        public static Graph<StationData> Construire(MySqlConnection connexion) {
            Graph<StationData> graph = new Graph<StationData>();
            Dictionary<int, Noeud<StationData>> nodesDict = new Dictionary<int, Noeud<StationData>>();


            MySqlCommand cmdStations = new MySqlCommand("SELECT station_id, libelle_station, longitude, latitude FROM stations_metro", connexion);
            MySqlDataReader readerStations = cmdStations.ExecuteReader();

            while (readerStations.Read()) {
                int id = readerStations.GetInt32(0);
                string name = readerStations.GetString(1);
                double longitude = readerStations.GetDouble(2);
                double latitude = readerStations.GetDouble(3);

                StationData data = new StationData {
                    Name = name,
                    Longitude = longitude,
                    Latitude = latitude
                };

                var node = new Noeud<StationData>(id, data);
                graph.AjouterNoeud(node);
                nodesDict[id] = node;

            }

            readerStations.Close();
            cmdStations.Dispose();

            MySqlCommand cmdConnexions = new MySqlCommand("SELECT station1_id, station2_id, distance_m FROM connexions_metro", connexion);
            MySqlDataReader readerConnexions = cmdConnexions.ExecuteReader();

            while (readerConnexions.Read()) {
                int id1 = readerConnexions.GetInt32(0);
                int id2 = readerConnexions.GetInt32(1);
                int length = Convert.ToInt32(readerConnexions.GetDouble(2));

                if (nodesDict.TryGetValue(id1, out var node1) && nodesDict.TryGetValue(id2, out var node2)) {
                    graph.AjouterLien(node1, node2, length);
                }
            }

            readerConnexions.Close();
            cmdConnexions.Dispose();

            AnsiConsole.Status()
                .Start("Creating graph...", ctx => {
                    try {
                        ctx.Spinner(Spinner.Known.Flip);
                        ctx.SpinnerStyle(Style.Parse("green"));
                        Shell.PrintSucces("Graph created successfully.");
                    }
                    catch (Exception ex) {
                        Shell.PrintError($"Import error: {ex.Message}");
                    }
                });
            return graph;

        }
    }
}
