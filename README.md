
# 📘 ParisShell Documentation

## ⚙️ Introduction

**ParisShell** est un terminal C# interactif pour la gestion d’un système complet de livraison alimentaire entre clients et cuisiniers à Paris. Il intègre des fonctions de gestion utilisateur, de visualisation de données, d'importation depuis Excel, et de navigation dans une base de données MySQL, le tout dans un environnement stylisé avec `Spectre.Console`.

---

## 💻 Lancement du Shell

```bash
dotnet run
```

Un écran d’accueil stylisé s'affiche avec un prompt type :
```bash
anon@paris:mysql:~$
```

---

## 📦 Commandes Disponibles

| Commande        | Description |
|-----------------|-------------|
| `help`          | Affiche toutes les commandes disponibles |
| `tuto`          | Lancer le tutoriel d'utilisation |
| `connect`       | Connexion manuelle à la base MySQL |
| `autoconnect`   | Connexion automatique à la base |
| `login`         | Connexion utilisateur |
| `logout`        | Déconnexion |
| `register`      | Création d'un nouveau compte |
| `user`          | Gestion des utilisateurs (ADMIN/BOZO) |
| `client`        | Commandes clients (`newc`, `orders`, `cancel`) |
| `cook`          | Accès cuisinier aux plats et ventes |
| `analytics`     | Données analytiques (admin) |
| `initdb`        | Crée la BDD, les tables, et importe les données |
| `graph`         | Affiche un graphe des stations de métro |
| `showtables`    | Liste des tables accessibles |
| `showtable`     | Affiche le contenu d’une table |
| `cinf`          | Infos de connexion SQL |
| `edit`          | Édition des infos utilisateurs |
| `clear`         | Nettoie le terminal |
| `exit`          | Quitte le shell |

---

## 🧑‍💼 Commandes Utilisateurs

```bash
user add
user update <userId>
user list
user getid
```

La commande `user` permet aux admins ou bozos d'ajouter, modifier ou afficher les utilisateurs. Elle utilise des interactions visuelles (prompts Spectre) pour la saisie.

---

## 🍽️ Commandes Client

```bash
client newc      # Passe une commande
client orders    # Affiche les commandes passées
client cancel    # Annule une commande (si EN_COURS)
```

Le client peut voir les plats, comparer le temps de livraison estimé (basé sur le graphe métro), et commander.

---

## 👨‍🍳 Commandes Cuisinier

```bash
cook clients
cook stats
cook platdujour
cook ventes
```

Permet au cuisinier de voir les plats qu’il a préparés, les ventes par type de plat, les clients servis, etc.

---

## 📈 Commandes Analytics

```bash
analytics delivery
analytics orders
analytics avg-price
analytics avg-acc
analytics client-orders
```

Ces commandes sont réservées à l’`ADMIN` ou au `BOZO`. Elles affichent sous forme de tableau des statistiques comme les livraisons, commandes, ou prix moyens.

---

## 🗃️ Base de Données & Import

La commande `initdb` :

```bash
initdb
```

- Crée toutes les tables
- Importe les données depuis :
  - `MetroParis.xlsx` pour les stations et connexions
  - `user.xlsx` pour les utilisateurs
  - `plats_simules.xlsx` pour les plats

Le processus affiche des barres de progression Spectre.Console personnalisées.

---

## 🗺️ Graphe des stations de métro

```bash
graph
```

Génère un graphe orienté avec SkiaSharp, basé sur la longitude/latitude réelle des stations. Prend en compte les distances entre stations (stockées dans `connexions_metro`). 

Des flèches indiquent la direction (graph orienté), et les nœuds sont colorés dynamiquement selon leur degré.

---

## 🔐 Sécurité & Sessions

- Authentification requise pour accéder à certaines commandes
- Rôles : `CLIENT`, `CUISINIER`, `ADMIN`, `BOZO`
- Les rôles déterminent l'accès aux tables (`showtables`) et aux commandes

---

## 🧪 Tests et Débogage

Utilise :

```bash
showtables
showtable <table>
cinf
```

Pour visualiser les données SQL et l’état de la connexion.

---

## 🎨 Interface et Style

ParisShell utilise `Spectre.Console` pour :

- Des prompts colorés
- Des barres de progression
- Des tableaux stylisés
- Des animations et spinners

---

## 📚 Astuces & Raccourcis

- `clear` : vide le terminal
- `edit` : modification ciblée des infos utilisateurs
- `graph` : utile pour visualiser les trajets entre cuisinier et client
- `getid` : récupère un ID utilisateur à partir de son nom/prénom
