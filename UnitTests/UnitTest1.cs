using ParisShell.Graph;
using System;
using System.Collections.Generic;
using Xunit;

namespace ParisShell.Test {
    public class GraphTests {
        [Fact]
        public void AjouterNoeud_ShouldAddNodeToGraph() {
            var graph = new Graph<string>();
            var noeud = new Noeud<string>(1, "A");
            graph.AjouterNoeud(noeud);
            var noeuds = graph.ObtenirNoeuds();
            Assert.Contains(noeud, noeuds);
        }

        [Fact]
        public void AjouterLien_ShouldConnectTwoNodes() {
            var graph = new Graph<string>();
            var n1 = new Noeud<string>(1, "A");
            var n2 = new Noeud<string>(2, "B");
            graph.AjouterNoeud(n1);
            graph.AjouterNoeud(n2);
            graph.AjouterLien(n1, n2, 10);

            // Use Dijkstra to verify connectivity
            var chemin = graph.DijkstraCheminPlusCourt(n1, n2);
            Assert.NotNull(chemin);
            Assert.Contains(n1, chemin);
            Assert.Contains(n2, chemin);
        }

        [Fact]
        public void DijkstraDistances_ReturnsCorrectDistance() {
            var graph = new Graph<string>();
            var a = new Noeud<string>(1, "A");
            var b = new Noeud<string>(2, "B");
            var c = new Noeud<string>(3, "C");
            graph.AjouterNoeud(a);
            graph.AjouterNoeud(b);
            graph.AjouterNoeud(c);
            graph.AjouterLien(a, b, 5);
            graph.AjouterLien(b, c, 7);

            var distances = graph.DijkstraDistances(a);
            Assert.Equal(0, distances[a]);
            Assert.Equal(5, distances[b]);
            Assert.Equal(12, distances[c]);
        }

        [Fact]
        public void EstConnexe_ShouldReturnTrueForConnectedGraph() {
            var graph = new Graph<string>();
            var a = new Noeud<string>(1, "A");
            var b = new Noeud<string>(2, "B");
            graph.AjouterNoeud(a);
            graph.AjouterNoeud(b);
            graph.AjouterLien(a, b, 1);

            Assert.True(graph.EstConnexe());
        }
    }
}
