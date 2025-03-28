using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;

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

        public void AfficherGraphique(string cheminFichier = "graph.png", int tailleImage = 1080) {
            if (noeuds.Count == 0) {
                Console.WriteLine("Le graphe est vide, impossible de l'afficher.");
                return;
            }

            // *** Définir le rayon max possible d’un nœud (utilisé pour calculer la marge)
            float rayonMax = 40f;

            // *** On appelle la fonction de positionnement en lui passant le rayonMax
            Dictionary<Noeud<T>, SKPoint> positions =
                CalculerPositionsForceDirected(tailleImage, rayonMax);

            using var surface = SKSurface.Create(new SKImageInfo(tailleImage, tailleImage));
            SKCanvas canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            // Styles pour dessin
            using var paintNoeud = new SKPaint {
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            using var paintLien = new SKPaint {
                Color = SKColors.Gray,
                StrokeWidth = 2,
                IsAntialias = true
            };

            using var paintTexte = new SKPaint {
                Color = SKColors.Black,
                IsAntialias = true
            };

            // Définir la police de texte avec taille correcte
            var font = new SKFont {
                Size = 16
            };

            // Dessiner les liens
            foreach (var lien in liens) {
                var p1 = positions[lien.Noeud1];
                var p2 = positions[lien.Noeud2];
                canvas.DrawLine(p1, p2, paintLien);
            }

            // Dessiner les noeuds
            foreach (var noeud in noeuds) {
                var pos = positions[noeud];

                // Taille du nœud (déjà plafonnée)
                float rayonNoeud = CalculerRayon(noeud);

                // Couleur en fonction du degré (exemple simplifié)
                int degre = liens.Count(l => l.Noeud1.Equals(noeud) || l.Noeud2.Equals(noeud));
                byte rouge = (byte)Math.Min(255, 50 + degre * 20);
                paintNoeud.Color = new SKColor(rouge, 100, (byte)(255 - rouge));

                // Dessin du cercle
                canvas.DrawCircle(pos, rayonNoeud, paintNoeud);

                // Petit offset vertical pour le label
                float labelOffsetY = -(rayonNoeud + 5f);

                // Dessin du texte
                canvas.DrawText(
                    noeud.Donnees.ToString(),
                    pos.X,
                    pos.Y + labelOffsetY,
                    SKTextAlign.Center,
                    font,
                    paintTexte
                );
            }

            // Export de l'image
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite(cheminFichier);
            data.SaveTo(stream);

            Console.WriteLine($"Graphique enregistré sous : {cheminFichier}");
        }

        // Exemple de calcul du rayon, au besoin (inchangé)
        private float CalculerRayon(Noeud<T> noeud) {
            // Calcul basique : 10 + 1.2 * degré, plafonné à 40
            int degre = liens.Count(l => l.Noeud1.Equals(noeud) || l.Noeud2.Equals(noeud));
            float rayonNoeud = 10 + degre * 1.2f;
            return Math.Min(rayonNoeud, 40f);
        }

        // *** On ajoute un paramètre rayonMax ici
        private Dictionary<Noeud<T>, SKPoint> CalculerPositionsForceDirected(
            int tailleImage,
            float rayonMax,
            int iterations = 1,
            float attraction = 0.01f,
            float repulsion = 3000f,
            float collisionMargin = 10f
        ) {
            Random rand = new Random();
            Dictionary<Noeud<T>, SKPoint> positions = new();
            Dictionary<Noeud<T>, SKPoint> vitesses = new();

            // 1) Initialisation des positions & vitesses
            foreach (var noeud in noeuds) {
                float x = rand.Next(100, tailleImage - 100);
                float y = rand.Next(100, tailleImage - 100);
                positions[noeud] = new SKPoint(x, y);
                vitesses[noeud] = new SKPoint(0, 0);
            }

            // 2) Boucle de simulation
            for (int step = 0; step < iterations; step++) {
                Dictionary<Noeud<T>, SKPoint> forces = noeuds.ToDictionary(n => n, n => new SKPoint(0, 0));

                // a) Répulsion globale (type Coulomb)
                foreach (var n1 in noeuds) {
                    foreach (var n2 in noeuds) {
                        if (n1.Equals(n2)) continue;

                        var p1 = positions[n1];
                        var p2 = positions[n2];
                        float dx = p1.X - p2.X;
                        float dy = p1.Y - p2.Y;
                        float dist2 = dx * dx + dy * dy + 0.01f; // évite division par zéro
                        float force = repulsion / dist2;

                        var f1 = forces[n1];
                        f1.X += dx * force;
                        f1.Y += dy * force;
                        forces[n1] = f1;
                    }
                }

                // b) Attraction (liens)
                foreach (var lien in liens) {
                    var p1 = positions[lien.Noeud1];
                    var p2 = positions[lien.Noeud2];
                    float dx = p2.X - p1.X;
                    float dy = p2.Y - p1.Y;

                    // Force d’attraction sur nœud1
                    var f1 = forces[lien.Noeud1];
                    f1.X += dx * attraction;
                    f1.Y += dy * attraction;
                    forces[lien.Noeud1] = f1;

                    // Force d’attraction inverse sur nœud2
                    var f2 = forces[lien.Noeud2];
                    f2.X -= dx * attraction;
                    f2.Y -= dy * attraction;
                    forces[lien.Noeud2] = f2;
                }

                // c) Collision (distance minimale = somme rayons + marge)
                foreach (var n1 in noeuds) {
                    float r1 = CalculerRayon(n1);
                    foreach (var n2 in noeuds) {
                        if (n1.Equals(n2)) continue;

                        float r2 = CalculerRayon(n2);
                        float minDist = r1 + r2 + collisionMargin;

                        var p1 = positions[n1];
                        var p2 = positions[n2];
                        float dx = p1.X - p2.X;
                        float dy = p1.Y - p2.Y;
                        float dist = (float)Math.Sqrt(dx * dx + dy * dy) + 0.01f;

                        if (dist < minDist) {
                            // Chevauchement
                            float overlap = minDist - dist;
                            float collisionForce = overlap * 2f;

                            var f1 = forces[n1];
                            f1.X += (dx / dist) * collisionForce;
                            f1.Y += (dy / dist) * collisionForce;
                            forces[n1] = f1;

                            var f2 = forces[n2];
                            f2.X -= (dx / dist) * collisionForce;
                            f2.Y -= (dy / dist) * collisionForce;
                            forces[n2] = f2;
                        }
                    }
                }

                // d) Mise à jour des positions (avec amortissement)
                float damping = 0.85f;
                foreach (var noeud in noeuds) {
                    var f = forces[noeud];
                    var v = vitesses[noeud];

                    // On ajoute la force à la vitesse, puis on amortit
                    v.X = (v.X + f.X) * damping;
                    v.Y = (v.Y + f.Y) * damping;
                    vitesses[noeud] = v;

                    // Mise à jour de la position
                    var p = positions[noeud];
                    p.X += v.X;
                    p.Y += v.Y;
                    positions[noeud] = p;
                }
            }

            // 3) Recentrer et mettre à l’échelle
            // *** On calcule la bounding box en tenant compte du rayonMax
            float minX = positions.Values.Min(p => p.X) - rayonMax;
            float maxX = positions.Values.Max(p => p.X) + rayonMax;
            float minY = positions.Values.Min(p => p.Y) - rayonMax;
            float maxY = positions.Values.Max(p => p.Y) + rayonMax;

            float largeurGraphe = maxX - minX;
            float hauteurGraphe = maxY - minY;

            // *** Ajuster la marge pour inclure le rayon au bord
            float marge = 10f; // marge “supplémentaire”

            // On calcule l’échelle horizontale et verticale
            float echelleX = (tailleImage - marge * 2) / largeurGraphe;
            float echelleY = (tailleImage - marge * 2) / hauteurGraphe;
            float echelle = Math.Min(echelleX, echelleY);

            // *** Soit on garde le zoom 1.2f, soit on le retire :
            // echelle *= 1.1f; // <-- à commenter/décommenter suivant ton besoin

            // Application de l’échelle et du décalage
            foreach (var noeud in noeuds) {
                var p = positions[noeud];
                p.X = (p.X - minX) * echelle + marge;
                p.Y = (p.Y - minY) * echelle + marge;
                positions[noeud] = p;
            }

            // (Optionnel) “Clamping” pour éviter de déborder (si on veut être 100% sûr)
            // foreach (var noeud in noeuds) {
            //    float r = CalculerRayon(noeud);

            //    var p = positions[noeud];

            //    p.X = Math.Clamp(p.X, r + 1, tailleImage - (r + 1));
            //    p.Y = Math.Clamp(p.Y, r + 1, tailleImage - (r + 1));
            //    positions[noeud] = p;
            // }

            return positions;
        }

    }

}

