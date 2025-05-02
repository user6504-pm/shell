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
        public List<Lien<T>> liens { get; }

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
//---------------------------------------------------Parcours du Graphe------------------------------------------------------------
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
        public List<Noeud<T>> ObtenirNoeuds() {
            return noeuds;
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

//---------------------------------------------------------------Plus Court Chemins----------------------------------------------------------------
        /// <summary>
        /// Implémentation de l'algorithme de Dijkstra pour trouver les distances minimales depuis un noeud source
        /// - Ne fonctionne qu'avec des poids positifs
        /// - Utilise une approche gloutonne (toujours étendre le chemin le plus prometteur)
        /// - Complexité O(n²)
        /// </summary>
        public Dictionary<Noeud<T>, int> DijkstraDistances(Noeud<T> depart) 
        {
            if (!noeuds.Contains(depart))
            {
                throw new ArgumentException("Le nœud de départ n'existe pas dans le graphe");
            }

            Dictionary<Noeud<T>, int> distances = new Dictionary<Noeud<T>, int>();
            Dictionary<Noeud<T>, bool> visites = new Dictionary<Noeud<T>, bool>();

            // Toutes les distances à +∞ sauf le départ
            foreach (var noeud in noeuds)
            {
                distances[noeud] = int.MaxValue;
                visites[noeud] = false;
            }
            distances[depart] = 0;

            while (visites.Values.Contains(false)) // Tant qu'il reste des noeuds non visités
            {
                // Sélection du noeud non visité avec distance minimale
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

                if (noeudActuel == null) // Cas des noeuds inaccessibles
                {
                    break;
                }

                visites[noeudActuel] = true;

                //Mise à jour des distances des voisins
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
                }
            }
            return distances;
        }

        /// <summary>
        /// Trouve le chemin le plus court entre deux noeuds avec Dijkstra
        /// </summary>
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
                return null; 
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

                // Sélection du noeud non visité le plus proche
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

                // Mise à jour des distances et prédécesseurs
                foreach (var lien in liens)
                {
                    if (lien.Noeud1 == noeudActuel)
                    {
                        Noeud<T> voisin = lien.Noeud2;
                        int nouvelleDistance = distances[noeudActuel] + lien.Poids;
                        if (nouvelleDistance < distances[voisin])
                        {
                            distances[voisin] = nouvelleDistance;
                            precedents[voisin] = noeudActuel; // On retient le prédécesseur
                        }
                    }
                }
            }

            if (precedents[arrivee] == null && depart != arrivee)
            {
                return null; // Aucun chemin trouvé
            }

            // Reconstruction du chemin
            List<Noeud<T>> chemin = new List<Noeud<T>>();
            for (Noeud<T> noeud = arrivee; noeud != null; noeud = precedents[noeud])
            {
                chemin.Insert(0, noeud);
            }
            return chemin; 
        }

        /// <summary>
        /// Implémentation de Bellman-Ford pour calculer les distances depuis une source
        /// - Gère les poids négatifs (mais pas les cycles négatifs accessibles)
        /// - Complexité O(n*m)
        /// - Basé sur le principe de relaxation des arêtes
        /// </summary>
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

            //Relaxation des arêtes
            for (int i = 0; i < noeuds.Count - 1; i++)
            {
                foreach (var lien in liens)
                {
                    if (distances[lien.Noeud1] != int.MaxValue && distances[lien.Noeud1] + lien.Poids < distances[lien.Noeud2])
                    {
                        distances[lien.Noeud2] = distances[lien.Noeud1] + lien.Poids;
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

        /// <summary>
        /// Calcule le plus court chemin entre deux noeuds en utilisant l'algorithme de Bellman-Ford
        /// </summary>
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

            // Toutes les distances à +∞, prédécesseurs à null
            foreach (Noeud<T> noeud in noeuds)
            {
                distances[noeud] = int.MaxValue;
                predecesseurs[noeud] = null; // Initialisation explicite des prédécesseurs
            }
            distances[depart] = 0;

            //Relaxation des arêtes
            for (int i = 0; i < noeuds.Count - 1; i++)
            {
                foreach (var lien in liens)
                {
                    if (distances[lien.Noeud1] != int.MaxValue && distances[lien.Noeud1] + lien.Poids < distances[lien.Noeud2])
                    {
                        distances[lien.Noeud2] = distances[lien.Noeud1] + lien.Poids;
                        predecesseurs[lien.Noeud2] = lien.Noeud1;
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

            //Reconstruction du chemin
            List<Noeud<T>> chemin = new List<Noeud<T>>();
            for (Noeud<T> noeud = arrivee; noeud != null; noeud = predecesseurs[noeud])
            {
                chemin.Insert(0, noeud);
            }
            return chemin;
        }
        /// <summary>
        /// Implémentation complète de l'algorithme de Floyd-Warshall:
        /// Pourquoi une méthode séparée ?
        /// - C'est le cœur algorithmique qui fait tout le travail lourd
        /// - Est réutilisée par les autres méthodes pour éviter la duplication de code
        /// - Calcule TOUTES les paires de distances en une seule passe
        /// - Respecte exactement le pseudo-code classique de Floyd-Warshall
        /// <returns>
        /// Tuple contenant :
        /// - distances: matrice de toutes les distances entre toutes les paires de noeuds
        /// - predecessors: matrice pour reconstruire les chemins
        /// </returns>
        public (Dictionary<(Noeud<T>, Noeud<T>), int> distances, Dictionary<(Noeud<T>, Noeud<T>), Noeud<T>> predecessors) FloydWarshallComplet()
        {
            Dictionary<(Noeud<T>, Noeud<T>), int> distances = new Dictionary<(Noeud<T>, Noeud<T>), int>();
            Dictionary<(Noeud<T>, Noeud<T>), Noeud<T>> predecessors = new Dictionary<(Noeud<T>, Noeud<T>), Noeud<T>>();

            // Initialisation de la diagonale à 0 et le reste à +∞
            foreach (var u in noeuds)
            {
                foreach (var v in noeuds)
                {
                    if (u == v)
                    {
                        distances[(u, v)] = 0;
                        predecessors[(u, v)] = null;
                    }
                    else
                    {
                        distances[(u, v)] = int.MaxValue;
                        predecessors[(u, v)] = null;
                    }
                }
            }

            // Remplissage avec les poids des arêtes existantes
            foreach (var lien in liens)
            {
                distances[(lien.Noeud1, lien.Noeud2)] = lien.Poids;
                predecessors[(lien.Noeud1, lien.Noeud2)] = lien.Noeud1;

                // Pour un graphe non orienté, on ajoute l'arrête inverse
                distances[(lien.Noeud2, lien.Noeud1)] = lien.Poids;
                predecessors[(lien.Noeud2, lien.Noeud1)] = lien.Noeud2;
            }

            foreach (var k in noeuds) // Noeud intermédiaire
            {
                foreach (var i in noeuds) // Noeud source
                {
                    foreach (var j in noeuds) // Noeud destination
                    {
                        // On évite les overflow en vérifiant les valeurs infinies
                        if (distances[(i, k)] != int.MaxValue && distances[(k, j)] != int.MaxValue)
                        {
                            int nouvelleDistance = distances[(i, k)] + distances[(k, j)];
                            // Si meilleur chemin trouvé via k
                            if (nouvelleDistance < distances[(i, j)])
                            {
                                distances[(i, j)] = nouvelleDistance;
                                predecessors[(i, j)] = predecessors[(k, j)];
                            }
                        }
                    }
                }
            }

            // n'arrivera jamais comme graphe poids de tous les noeuds > 0
            foreach (var noeud in noeuds)
            {
                if (distances[(noeud, noeud)] < 0)
                {
                    throw new InvalidOperationException("Le graphe contient un cycle de poids négatif.");
                }
            }

            return (distances, predecessors);
        }


        /// <summary>
        /// Calcule les distances les plus courtes depuis un noeud source vers tous les autres noeuds en utilisant l'algorithme de Floyd-Warshall
        /// - Fournit une interface simple pour obtenir juste les distances depuis un noeud
        /// - Réutilise le calcul complet pour éviter de refaire les calculs
        /// </summary>
        /// <param name="depart">Noeud source à partir duquel calculer les distances</param>
        /// <returns>Dictionnaire des distances vers chaque noeud</returns>
        public Dictionary<Noeud<T>, int> FloydWarshallDistances(Noeud<T> depart)
        {
            // On utilise la méthode complète qui fait tout le travail
            (Dictionary<(Noeud<T>, Noeud<T>), int> distances, Dictionary <(Noeud<T>, Noeud<T>), Noeud <T>> predecessors) = FloydWarshallComplet();

            // On extrait juste les distances depuis le noeud de départ
            Dictionary<Noeud<T>, int> result = new Dictionary<Noeud<T>, int>();
            foreach (var noeud in noeuds)
            {
                result[noeud] = distances[(depart, noeud)];
            }
            return result;
        }

        /// <summary>
        /// Reconstruit le chemin le plus court entre deux noeuds en utilisant Floyd-Warshall
        /// </summary>
        /// <param name="depart">Noeud de départ</param>
        /// <param name="arrivee">Noeud d'arrivée</param>
        /// <returns>Liste des noeuds formant le chemin (vide si pas de chemin)</returns>
        public List<Noeud<T>> FloydWarshallCheminPlusCourt(Noeud<T> depart, Noeud<T> arrivee)
        {
          
            (Dictionary<(Noeud<T>, Noeud<T>), int> distances, Dictionary<(Noeud<T>, Noeud<T>), Noeud<T>> predecessors) = FloydWarshallComplet();

           
            if (distances[(depart, arrivee)] == int.MaxValue)
            {
                return new List<Noeud<T>>(); 
            }

            List<Noeud<T>> chemin = new List<Noeud<T>>();
            Noeud<T> courant = arrivee;

            while (courant != null && courant != depart)
            {
                chemin.Insert(0, courant);
                courant = predecessors[(depart, courant)];
            }

            if (courant == null)
            {
                return new List<Noeud<T>>(); 
            }

            chemin.Insert(0, depart);
            return chemin;
        }
        // ------------------------------------------------Coloration de graphe----------------------------------------------------------
        public int GetDegre(Noeud<T> noeud)
        {
            int degre = 0;
            foreach (var lien in liens)
            {
                if (lien.Noeud1 == noeud || lien.Noeud2 == noeud) degre++;
            }
            return degre;
        }
        public List<Noeud<T>> GetVoisins(Noeud<T> noeud)
        {
            var voisins = new List<Noeud<T>>();
            foreach (var lien in liens)
            {
                if (lien.Noeud1 == noeud)
                {
                    voisins.Add(lien.Noeud2);
                }
                else if (lien.Noeud2 == noeud)
                {
                    voisins.Add(lien.Noeud1);
                }
            }
            return voisins;
        }
        public bool EstVoisin(Noeud<T> a, Noeud<T> b)
        {
            foreach (var lien in liens)
            {
                if (lien.Noeud1 == a && lien.Noeud2 == b)
                {
                    return true;
                }
                else if (lien.Noeud1 == b && lien.Noeud2 == a)
                {
                    return true;
                }
            }
            return false;
        }
        public List<Noeud<T>> TrierNoeudsParDegreDécroissant_Insertion()
        {
            List<Noeud<T>> noeudsTries = new List<Noeud<T>>(noeuds);

            for (int i = 1; i < noeudsTries.Count; i++)
            {
                var cle = noeudsTries[i];
                int j = i - 1;

                while (j >= 0 && GetDegre(noeudsTries[j]) < GetDegre(cle))
                {
                    noeudsTries[j + 1] = noeudsTries[j];
                    j--;
                }
                noeudsTries[j + 1] = cle;
            }

            return noeudsTries;
        }
        public Dictionary<Noeud<T>, int> Welsh_Powell()
        {
            var noeudsTries = TrierNoeudsParDegreDécroissant_Insertion();
            var Coloration = new Dictionary<Noeud<T>, int>();
            int couleurActuelle = 0;

            foreach (var Noeud1 in noeudsTries)
            {
                if (!Coloration.ContainsKey(Noeud1))
                {
                    couleurActuelle++;
                    Coloration[Noeud1] = couleurActuelle; //coloration d'un premier noeud avec une couleur

                    foreach (var Noeud2 in noeudsTries)
                    {
                        //vérifier les noeuds qui ne sont pas colorié et qui ne sont pas voisin au premier noeud colorié
                        if (!Coloration.ContainsKey(Noeud2) && !EstVoisin(Noeud1, Noeud2))
                        {
                            bool peutColorier = true;
                            //deuxième vérification : vérifier que ses voisins ne sont pas déjà colorié de cette même couleur
                            foreach (var voisin in GetVoisins(Noeud2))
                            {
                                if (Coloration.ContainsKey(voisin) && Coloration[voisin] == couleurActuelle)
                                {
                                    peutColorier = false;
                                    break;
                                }
                            }

                            if (peutColorier)
                            {
                                Coloration[Noeud2] = couleurActuelle;
                            }
                        }
                    }
                }
            }
            return Coloration;
        }

        public int EstimerNombreChromatique(Dictionary<Noeud<T>, int> coloration)
        {
            List<int> CouleursUniques = new List<int>();
            foreach (var couleur in coloration)
            {
                if (!CouleursUniques.Contains(couleur.Value))
                {
                    CouleursUniques.Add(couleur.Value);
                }
            }
            int nombreChromatique = CouleursUniques.Count;
            return nombreChromatique;

        }
        public bool EstimerGrapheBiparti(int nombreChromatique)
        {
            if (nombreChromatique <= 2)
            {
                return true;
            }
            return false;
        }

        public bool EstimerGraphePlanaire(int nombreChromatique)
        {
            if (nombreChromatique <= 4)
            {
                return true;
            }
            return false;
        }

        public Dictionary<int, List<Noeud<T>>> TrouverGroupesIndependants(Dictionary<Noeud<T>, int> coloration)
        {
            var groupes = new Dictionary<int, List<Noeud<T>>>();

            foreach (var noeudColoré in coloration)
            {
                if (!groupes.ContainsKey(noeudColoré.Value))
                {
                    groupes[noeudColoré.Value] = new List<Noeud<T>>();
                }
                groupes[noeudColoré.Value].Add(noeudColoré.Key);
            }
            return groupes;
        }

        public void AnalyserResultatsColoration()
        {
            Dictionary<Noeud<T>, int> coloration = Welsh_Powell();

            if (coloration == null || coloration.Count == 0)
            {
                throw new InvalidOperationException("Aucun nœud n'a été coloré.");
            }

            Console.WriteLine("\n======Nombre minimal de couleurs=====");
            int nombreChromatique = EstimerNombreChromatique(coloration);
            Console.WriteLine("Nombre minimal de couleurs: " + nombreChromatique);

            Console.WriteLine("\n=====Graphe Biparti=====");
            bool EstBiparti = EstimerGrapheBiparti(nombreChromatique);
            if (EstBiparti)
            {
                Console.WriteLine("Le graphe est biparti\nJustification : Au plus, 2 couleurs suffisent pour colorer le graphe");
            }
            else
            {
                Console.WriteLine("Le graphe n'est pas biparti\nJustification : Il nécessite plus de 2 couleurs pour colorer le graphe");
            }

            Console.WriteLine("\n=====Graphe Planaire=====");
            bool EstPlanaire = EstimerGraphePlanaire(nombreChromatique);
            if (EstPlanaire)
            {
                Console.WriteLine("D'après le Théorème des 4 couleurs, le graphe est probablement planaire");
            }
            else
            {
                Console.WriteLine("D'après le Théorème des 4 couleurs, le graphe n'est probablement pas planaire");
            }

            Console.WriteLine("\n=====Groupe indépendants=====");
            var GroupeIndépendant = TrouverGroupesIndependants(coloration);
            foreach (var Groupe in GroupeIndépendant)
            {
                string ligne = "Groupe " + Groupe.Key + ": [";
                foreach (var noeud in Groupe.Value)
                {
                    ligne += noeud.Id + ",";
                }
                ligne = ligne.TrimEnd(',');
                ligne += "]";
                Console.WriteLine(ligne);
            }

        }

        //-----------------------------------------------------------Autres-------------------------------------------------------------------------------
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

        public int TempsCheminStations(List<Noeud<StationData>> chemin) {
            if (chemin == null || chemin.Count < 2)
               return 5; //5 étant le temps de livraison, car même si le cuisinier et l'utilisateur sont à la même station, on prends en compte un temps

            const decimal vitesseKmH = 15m;
            double distanceTotaleKm = 0;

            for (int i = 0; i < chemin.Count - 1; i++) {
                var from = chemin[i];
                var to = chemin[i + 1];

                var lien = liens.FirstOrDefault(l =>
                    (l.Noeud1.Equals(from) && l.Noeud2.Equals(to)) ||
                    (l.Noeud1.Equals(to) && l.Noeud2.Equals(from)));

                if (lien != null) {
                    distanceTotaleKm += lien.Poids / 1000.0;
                }
                else {
                    Shell.PrintWarning($"Missing link between {from.Donnees.Name} and {to.Donnees.Name}");
                }
            }

            decimal tempsHeures = (decimal)distanceTotaleKm / vitesseKmH;
            decimal tempsMinutes = tempsHeures * 60;
            int result = Convert.ToInt32(Math.Round(tempsMinutes) + 10); //10 étant le temps moyen de changement par trajet plus le temps de livraison
            return result; 
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
                    canvas.DrawText(station.Name, point.X + radius + 2, point.Y - 2, textPaint);
                }

            }

            Shell.PrintSucces($"Graphe SVG exporté avec flèches : {cheminFichier}");
        }
    }
}

