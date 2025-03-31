using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParisShell.Graph {
    internal class Noeud<T> {
        public int Id { get; set; }
        public T Donnees { get; set; }

        public Noeud(int id, T donnees) {
            Id = id;
            Donnees = donnees;
        }

        public override string ToString() {
            return $"Noeud[ID={Id}, Donnees={Donnees}]";
        }

    }
}
