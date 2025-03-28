using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParisShell.Graph {
    internal class Lien<T> {
        public Noeud<T> Noeud1 { get; }
        public Noeud<T> Noeud2 { get; }
        public int Poids { get; set; }

        public Lien(Noeud<T> noeud1, Noeud<T> noeud2, int poids) {
            Noeud1 = noeud1;
            Noeud2 = noeud2;
            Poids = poids;
        }

        public override string ToString() {
            return $"Lien[De {Noeud1.Donnees} à {Noeud2.Donnees}, Poids={Poids}]";
        }
    }
}
