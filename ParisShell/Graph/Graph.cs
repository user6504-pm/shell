using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using ParisShell.Models;
using System.IO;
namespace ParisShell.Graph {
    internal class Graph<T> {
        private List<Noeud<T>> noeuds;
        private List<Lien<T>> liens;

        public Graph() {
            noeuds = new List<Noeud<T>>();
            liens = new List<Lien<T>>();
        }

        public void AjouterNoeud(Noeud<T> noeud) {
            noeuds.Add(noeud);
        }

        public void AjouterLien(Noeud<T> noeud1, Noeud<T> noeud2, int poids) {
            if (noeuds.Contains(noeud1) && noeuds.Contains(noeud2)) {
                liens.Add(new Lien<T>(noeud1, noeud2, poids));
            }
            else {
                throw new Exception("Les nœuds doivent être présents dans le graphe avant d'ajouter un lien.");
            }
        }

        public void AfficherGraphe() {
            Console.WriteLine("Noeuds du graphe:");
            foreach (var noeud in noeuds) {
                Console.WriteLine(noeud);
            }

            Console.WriteLine("\nLiens du graphe:");
            foreach (var lien in liens) {
                Console.WriteLine(lien);
            }
        }

        public void ParcoursLargeur(Noeud<T> depart) {
            if (!noeuds.Contains(depart)) {
                Console.WriteLine("Le nœud de départ n'existe pas dans le graphe.");
                return;
            }

            HashSet<Noeud<T>> visite = new HashSet<Noeud<T>>();
            Queue<Noeud<T>> file = new Queue<Noeud<T>>();

            file.Enqueue(depart);
            visite.Add(depart);

            Console.WriteLine("Parcours en Largeur (BFS) :");

            while (file.Count > 0) {
                var noeudActuel = file.Dequeue();
                Console.Write(noeudActuel.Donnees + " ");

                foreach (var lien in liens) {
                    if (lien.Noeud1.Equals(noeudActuel) && !visite.Contains(lien.Noeud2)) {
                        file.Enqueue(lien.Noeud2);
                        visite.Add(lien.Noeud2);
                    }
                    else if (lien.Noeud2.Equals(noeudActuel) && !visite.Contains(lien.Noeud1)) {
                        file.Enqueue(lien.Noeud1);
                        visite.Add(lien.Noeud1);
                    }
                }
            }
            Console.WriteLine();
        }

        public void ParcoursProfondeur(Noeud<T> depart) {
            if (!noeuds.Contains(depart)) {
                Console.WriteLine("Le nœud de départ n'existe pas dans le graphe.");
                return;
            }

            HashSet<Noeud<T>> visite = new HashSet<Noeud<T>>();
            Stack<Noeud<T>> pile = new Stack<Noeud<T>>();

            pile.Push(depart);
            visite.Add(depart);

            Console.WriteLine("Parcours en Profondeur (DFS) :");

            while (pile.Count > 0) {
                var noeudActuel = pile.Pop();
                Console.Write(noeudActuel.Donnees + " ");

                foreach (var lien in liens) {
                    if (lien.Noeud1.Equals(noeudActuel) && !visite.Contains(lien.Noeud2)) {
                        pile.Push(lien.Noeud2);
                        visite.Add(lien.Noeud2);
                    }
                    else if (lien.Noeud2.Equals(noeudActuel) && !visite.Contains(lien.Noeud1)) {
                        pile.Push(lien.Noeud1);
                        visite.Add(lien.Noeud1);
                    }
                }
            }
            Console.WriteLine();
        }

        public bool EstConnexe() {
            if (noeuds.Count == 0) return true;

            var visite = new HashSet<Noeud<T>>();
            var pile = new Stack<Noeud<T>>();

            var premier = noeuds[0];
            pile.Push(premier);
            visite.Add(premier);

            while (pile.Count > 0) {
                var noeud = pile.Pop();

                foreach (var lien in liens) {
                    Noeud<T> voisin = null;
                    if (lien.Noeud1.Equals(noeud)) voisin = lien.Noeud2;
                    else if (lien.Noeud2.Equals(noeud)) voisin = lien.Noeud1;

                    if (voisin != null && !visite.Contains(voisin)) {
                        pile.Push(voisin);
                        visite.Add(voisin);
                    }
                }
            }

            return visite.Count == noeuds.Count;
        }

        public bool ADesCircuits() {
            var visite = new HashSet<Noeud<T>>();

            foreach (var noeud in noeuds) {
                if (!visite.Contains(noeud)) {
                    if (DetecterCycleDFS(noeud, null, visite))
                        return true;
                }
            }
            return false;
        }

        private bool DetecterCycleDFS(Noeud<T> noeud, Noeud<T> parent, HashSet<Noeud<T>> visite) {
            visite.Add(noeud);

            foreach (var lien in liens) {
                Noeud<T> voisin = null;
                if (lien.Noeud1.Equals(noeud)) voisin = lien.Noeud2;
                else if (lien.Noeud2.Equals(noeud)) voisin = lien.Noeud1;

                if (voisin != null) {
                    if (!visite.Contains(voisin)) {
                        if (DetecterCycleDFS(voisin, noeud, visite))
                            return true;
                    }
                    else if (!voisin.Equals(parent)) {
                        return true;
                    }
                }
            }

            return false;
        }
        public Dictionary<Noeud<T>, int> DijkstraDistances(Noeud<T> depart) //Dictio de distance par rapport à un noeud (départ)
        {
            if (!noeuds.Contains(depart))
            {
                throw new ArgumentException("Le nœud de départ n'existe pas dans le graphe");
            }

            Dictionary<Noeud<T>, int> distances = new Dictionary<Noeud<T>, int>();
            Dictionary<Noeud<T>, bool> visites = new Dictionary<Noeud<T>, bool>();

            foreach (var noeud in noeuds)
            {
                distances[noeud] = int.MaxValue;
                visites[noeud] = false;
            }
            distances[depart] = 0;

            while (visites.Values.Contains(false))
            {
                Noeud<T> noeudActuel = null;
                int distanceMin = int.MaxValue;

                foreach (var noeud in noeuds)
                {
                    if (!visites[noeud] && distances[noeud] < distanceMin)
                    {
                        distanceMin = distances[noeud];
                        noeudActuel = noeud;
                    }
                }

                if (noeudActuel == null)
                {
                    break;
                }

                visites[noeudActuel] = true;

                foreach (var lien in liens)
                {
                    if (lien.Noeud1 == noeudActuel)
                    {
                        Noeud<T> voisin = lien.Noeud2;
                        int nouvelleDistance = distances[noeudActuel] + lien.Poids;
                        if (nouvelleDistance < distances[voisin])
                        {
                            distances[voisin] = nouvelleDistance;
                        }
                    }
                    else if (lien.Noeud2 == noeudActuel)
                    {
                        Noeud<T> voisin = lien.Noeud1;
                        int nouvelleDistance = distances[noeudActuel] + lien.Poids;
                        if (nouvelleDistance < distances[voisin])
                        {
                            distances[voisin] = nouvelleDistance;
                        }
                    }
                }
            }
            return distances;
        }

        public List<Noeud<T>> DijkstraCheminPlusCourt(Noeud<T> depart, Noeud<T> arrivee) //Liste des noeuds traversé par le plus court chemin
        {
            if (!noeuds.Contains(depart))
            {
                throw new ArgumentException("Le nœud de départ n'existe pas dans le graphe");
            }
            if (!noeuds.Contains(arrivee))
            {
                throw new ArgumentException("Le nœud d'arrivée n'existe pas dans le graphe");
            }

            if (depart == arrivee)
            {
                return null; // pas de chemin
            }

            Dictionary<Noeud<T>, int> distances = new Dictionary<Noeud<T>, int>();
            Dictionary<Noeud<T>, Noeud<T>> precedents = new Dictionary<Noeud<T>, Noeud<T>>();
            HashSet<Noeud<T>> visites = new HashSet<Noeud<T>>();

            foreach (var noeud in noeuds)
            {
                distances[noeud] = int.MaxValue;
                precedents[noeud] = null;
            }
            distances[depart] = 0;

            while (visites.Count < noeuds.Count)
            {
                Noeud<T> noeudActuel = null;
                int distanceMin = int.MaxValue;

                foreach (var noeud in noeuds)
                {
                    if (!visites.Contains(noeud) && distances[noeud] < distanceMin)
                    {
                        distanceMin = distances[noeud];
                        noeudActuel = noeud;
                    }
                }

                if (noeudActuel == null)
                    break;

                visites.Add(noeudActuel);

                foreach (var lien in liens)
                {
                    if (lien.Noeud1 == noeudActuel)
                    {
                        Noeud<T> voisin = lien.Noeud2;
                        int nouvelleDistance = distances[noeudActuel] + lien.Poids;
                        if (nouvelleDistance < distances[voisin])
                        {
                            distances[voisin] = nouvelleDistance;
                            precedents[voisin] = noeudActuel;
                        }
                    }
                    else if (lien.Noeud2 == noeudActuel)
                    {
                        Noeud<T> voisin = lien.Noeud1;
                        int nouvelleDistance = distances[noeudActuel] + lien.Poids;
                        if (nouvelleDistance < distances[voisin])
                        {
                            distances[voisin] = nouvelleDistance;
                            precedents[voisin] = noeudActuel;
                        }
                    }
                }
            }

            if (precedents[arrivee] == null && depart != arrivee)
            {
                return null; // Aucun chemin trouvé
            }

            List<Noeud<T>> chemin = new List<Noeud<T>>();
            for (Noeud<T> noeud = arrivee; noeud != null; noeud = precedents[noeud])
            {
                chemin.Insert(0, noeud);
            }
            return chemin; 
        }

       

        public Dictionary<Noeud<T>, int> BellmanFordDistance(Noeud<T> depart) //Dictio de distance par rapport à un noeud (départ)
        {
            if (!noeuds.Contains(depart))
            {
                throw new ArgumentException("Le nœud de départ n'existe pas dans le graphe");
            }

            Dictionary<Noeud<T>, int> distances = new Dictionary<Noeud<T>, int>();
            foreach (Noeud<T> noeud in noeuds)
            {
                distances[noeud] = int.MaxValue; // Toutes les distances sont infinies au départ
            }
            distances[depart] = 0;

            for (int i = 0; i < noeuds.Count - 1; i++)
            {
                foreach (var lien in liens)
                {
                    if (distances[lien.Noeud1] != int.MaxValue && distances[lien.Noeud1] + lien.Poids < distances[lien.Noeud2])
                    {
                        distances[lien.Noeud2] = distances[lien.Noeud1] + lien.Poids;
                    }
                    if (distances[lien.Noeud2] != int.MaxValue && distances[lien.Noeud2] + lien.Poids < distances[lien.Noeud1])
                    {
                        distances[lien.Noeud1] = distances[lien.Noeud2] + lien.Poids;
                    }
                }
            }

            // Détection des cycles négatifs (en théorie il y en aura jamais)
            foreach (var lien in liens)
            {
                if (distances[lien.Noeud1] != int.MaxValue && distances[lien.Noeud1] + lien.Poids < distances[lien.Noeud2])
                {
                    throw new InvalidOperationException("Le graphe contient un cycle de poids négatif.");
                }
            }

            return distances;
        }

        public List<Noeud<T>> BellmanFordCheminPlusCourt(Noeud<T> depart, Noeud<T> arrivee) //Liste des noeuds traversé par le plus court chemin
        {
            if (!noeuds.Contains(depart))
            {
                throw new ArgumentException("Le nœud de départ n'existe pas dans le graphe");
            }

            if (!noeuds.Contains(arrivee))
            {
                throw new ArgumentException("Le nœud d'arrivée n'existe pas dans le graphe");
            }

            if (depart == arrivee)
            {
                return null; // pas de chemin
            }

            Dictionary<Noeud<T>, int> distances = new Dictionary<Noeud<T>, int>();
            Dictionary<Noeud<T>, Noeud<T>> predecesseurs = new Dictionary<Noeud<T>, Noeud<T>>();

            foreach (Noeud<T> noeud in noeuds)
            {
                distances[noeud] = int.MaxValue;
                predecesseurs[noeud] = null; // Initialisation explicite des prédécesseurs
            }
            distances[depart] = 0;

            for (int i = 0; i < noeuds.Count - 1; i++)
            {
                foreach (var lien in liens)
                {
                    if (distances[lien.Noeud1] != int.MaxValue && distances[lien.Noeud1] + lien.Poids < distances[lien.Noeud2])
                    {
                        distances[lien.Noeud2] = distances[lien.Noeud1] + lien.Poids;
                        predecesseurs[lien.Noeud2] = lien.Noeud1;
                    }
                    if (distances[lien.Noeud2] != int.MaxValue && distances[lien.Noeud2] + lien.Poids < distances[lien.Noeud1])
                    {
                        distances[lien.Noeud1] = distances[lien.Noeud2] + lien.Poids;
                        predecesseurs[lien.Noeud1] = lien.Noeud2;
                    }
                }
            }

            // Détection des cycles négatifs (en théorie il y en aura jamais)
            foreach (var lien in liens)
            {
                if (distances[lien.Noeud1] != int.MaxValue && distances[lien.Noeud1] + lien.Poids < distances[lien.Noeud2])
                {
                    throw new InvalidOperationException("Le graphe contient un cycle de poids négatif.");
                }
            }

            List<Noeud<T>> chemin = new List<Noeud<T>>();
            for (Noeud<T> noeud = arrivee; noeud != null; noeud = predecesseurs[noeud])
            {
                chemin.Insert(0, noeud);
            }
            return chemin;
        }

        public Dictionary<(Noeud<T>, Noeud<T>), int> FloydWarshallDistance() // Dictio == (Noeud1,Noeud2, distance entre Noeud1 et Noeud2)
        {
            return null; //à compléter
        }

        public List<Noeud<T>> FloydWarshallCheminPlusCourt(Noeud<T> source, Noeud<T> target) //Liste des noeuds traversé par le plus court chemin
        {
            return null; //à compléter
        }
        public void ObtenirCaracteristiques() {
            int nombreNoeuds = noeuds.Count;
            int nombreLiens = liens.Count;

            Console.WriteLine("===== Caractéristiques du Graphe =====");
            Console.WriteLine($"Nombre de nœuds : {nombreNoeuds}");
            Console.WriteLine($"Nombre de liens : {nombreLiens}");

            double densite = (nombreNoeuds > 1) ? (2.0 * nombreLiens) / (nombreNoeuds * (nombreNoeuds - 1)) : 0;
            Console.WriteLine($"Densité du graphe : {densite:F4}");
            Console.WriteLine($"Le graphe est connexe ? : {(EstConnexe() ? "Oui" : "Non")}");
            Console.WriteLine($"Le graphe contient des circuits ? : {(ADesCircuits() ? "Oui" : "Non")}");

            var degres = noeuds.ToDictionary(n => n, n => 0);
            foreach (var lien in liens) {
                degres[lien.Noeud1]++;
                degres[lien.Noeud2]++;
            }

            int degreMin = degres.Values.Min();
            int degreMax = degres.Values.Max();
            double degreMoyen = degres.Values.Average();

            Console.WriteLine($"Degré minimum : {degreMin}");
            Console.WriteLine($"Degré maximum : {degreMax}");
            Console.WriteLine($"Degré moyen : {degreMoyen:F2}");
        }

        public void ExporterSvg(string cheminFichier = "graph_geo.svg", List<Noeud<T>> chemin = null, int width = 1920, int height = 1080) {
            if (File.Exists(cheminFichier)) File.Delete(cheminFichier);

            if (noeuds.Count == 0) {
                Console.WriteLine("Le graphe est vide.");
                return;
            }

            if (!(noeuds.First().Donnees is StationData)) {
                Shell.PrintError("Graphique géographique seulement possible avec des données StationData.");
                return;
            }

            double minLon = noeuds.Min(n => ((StationData)(object)n.Donnees).Longitude);
            double maxLon = noeuds.Max(n => ((StationData)(object)n.Donnees).Longitude);
            double minLat = noeuds.Min(n => ((StationData)(object)n.Donnees).Latitude);
            double maxLat = noeuds.Max(n => ((StationData)(object)n.Donnees).Latitude);
            float margin = 40f;

            using var stream = File.OpenWrite(cheminFichier);
            var bounds = new SKRect(0, 0, width, height);
            using var canvas = SKSvgCanvas.Create(bounds, stream);

            var backgroundPaint = new SKPaint {
                Color = SKColors.White,
                Style = SKPaintStyle.Fill
            };
            canvas.DrawRect(bounds, backgroundPaint);

            var edgePaint = new SKPaint {
                Color = new SKColor(106, 90, 205),
                StrokeWidth = 1.5f, // plus fin
                IsAntialias = true
            };

            var textPaint = new SKPaint {
                Color = SKColors.Black,
                TextSize = 12,
                IsAntialias = true
            };

            float scale = 1f; // zoom bien plus fort
            float rangeLon = (float)(maxLon - minLon);
            float rangeLat = (float)(maxLat - minLat);

            SKPoint Convert(StationData s) {
                float x = (float)((s.Longitude - minLon - rangeLon / 2) * scale + rangeLon / 2) / rangeLon * (width - 2 * margin) + margin;
                float y = (float)((maxLat - s.Latitude - rangeLat / 2) * scale + rangeLat / 2) / rangeLat * (height - 2 * margin) + margin;
                return new SKPoint(x, y);
            }

            var points = new Dictionary<object, SKPoint>();
            foreach (var noeud in noeuds) {
                var station = (StationData)(object)noeud.Donnees;
                points[noeud] = Convert(station);
            }

            var degres = noeuds.ToDictionary(
                n => n,
                n => liens.Count(l => l.Noeud1 == n || l.Noeud2 == n)
            );
            int maxDegre = degres.Values.Max();

            var rayonNoeud = new Dictionary<object, float>();

            foreach (var noeud in noeuds) {
                int degre = degres[noeud];
                float radius = 1.5f + degre * 0.7f;
                rayonNoeud[noeud] = radius;
            }

            void DrawArrowFromNodeToNode(object n1, object n2) {
                var from = points[n1];
                var to = points[n2];
                var r1 = rayonNoeud[n1];
                var r2 = rayonNoeud[n2];


                var dx = to.X - from.X;
                var dy = to.Y - from.Y;
                var len = (float)Math.Sqrt(dx * dx + dy * dy);
                if (len < 1e-5) return;

                var ux = dx / len;
                var uy = dy / len;

                var start = new SKPoint(from.X + ux * r1, from.Y + uy * r1);
                var end = new SKPoint(to.X - ux * r2, to.Y - uy * r2);

                canvas.DrawLine(start, end, edgePaint);

                float arrowSize = 6f;
                var angle = Math.Atan2(end.Y - start.Y, end.X - start.X);
                var sin = (float)Math.Sin(angle);
                var cos = (float)Math.Cos(angle);

                var p1 = new SKPoint(end.X - arrowSize * cos + arrowSize / 2 * sin, end.Y - arrowSize * sin - arrowSize / 2 * cos);
                var p2 = new SKPoint(end.X - arrowSize * cos - arrowSize / 2 * sin, end.Y - arrowSize * sin + arrowSize / 2 * cos);

                var arrowPath = new SKPath();
                arrowPath.MoveTo(end);
                arrowPath.LineTo(p1);
                arrowPath.LineTo(p2);
                arrowPath.Close();
                canvas.DrawPath(arrowPath, edgePaint);
            }

            HashSet<(object, object)> arcsChemin = new();

            if (chemin != null && chemin.Count >= 2) {
                for (int i = 0; i < chemin.Count - 1; i++) {
                    arcsChemin.Add((chemin[i], chemin[i + 1]));
                }
            }

            foreach (var lien in liens) {
                if (arcsChemin.Contains((lien.Noeud1, lien.Noeud2))) continue;
                if (arcsChemin.Contains((lien.Noeud2, lien.Noeud1))) continue;
                DrawArrowFromNodeToNode(lien.Noeud1, lien.Noeud2);
            }


            if (chemin != null && chemin.Count >= 2) {
                var highlightPaint = new SKPaint {
                    Color = SKColors.Red,
                    StrokeWidth = 2,
                    IsAntialias = true
                };

                void DrawHighlightArrow(object n1, object n2) {
                    var from = points[n1];
                    var to = points[n2];
                    var r1 = rayonNoeud[n1];
                    var r2 = rayonNoeud[n2];

                    var dx = to.X - from.X;
                    var dy = to.Y - from.Y;
                    var len = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (len < 1e-5) return;

                    var ux = dx / len;
                    var uy = dy / len;

                    var start = new SKPoint(from.X + ux * r1, from.Y + uy * r1);
                    var end = new SKPoint(to.X - ux * r2, to.Y - uy * r2);

                    canvas.DrawLine(start, end, highlightPaint);

                    // Tête de flèche rouge
                    float arrowSize = 9;
                    var angle = Math.Atan2(end.Y - start.Y, end.X - start.X);
                    var sin = (float)Math.Sin(angle);
                    var cos = (float)Math.Cos(angle);

                    var p1 = new SKPoint(end.X - arrowSize * cos + arrowSize / 2 * sin, end.Y - arrowSize * sin - arrowSize / 2 * cos);
                    var p2 = new SKPoint(end.X - arrowSize * cos - arrowSize / 2 * sin, end.Y - arrowSize * sin + arrowSize / 2 * cos);

                    var arrowPath = new SKPath();
                    arrowPath.MoveTo(end);
                    arrowPath.LineTo(p1);
                    arrowPath.LineTo(p2);
                    arrowPath.Close();
                    canvas.DrawPath(arrowPath, highlightPaint);
                }

                for (int i = 0; i < chemin.Count - 1; i++) {
                    DrawHighlightArrow(chemin[i], chemin[i + 1]);
                }
            }

            foreach (var noeud in noeuds) {
                var station = (StationData)(object)noeud.Donnees;
                var point = points[noeud];
                int degre = degres[noeud];
                float radius = rayonNoeud[noeud];

                byte intensity = (byte)(255 - (degre * 200 / Math.Max(1, maxDegre)));
                var color = new SKColor((byte)(180 - intensity / 2), (byte)(20 + intensity), (byte)(130 + intensity / 4));

                var nodePaint = new SKPaint {
                    Color = color,
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                };

                canvas.DrawCircle(point, radius, nodePaint);

                bool estExtremite = chemin != null && (noeud.Equals(chemin.First()) || noeud.Equals(chemin.Last()));

                if (degre >= 10 || estExtremite) {
                    canvas.DrawText(station.Nom, point.X + radius + 2, point.Y - 2, textPaint);
                }

            }

            Shell.PrintSucces($"Graphe SVG exporté avec flèches : {cheminFichier}");
        }
    }
}

