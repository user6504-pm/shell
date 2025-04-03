
# ParisShell Documentation

## Overview

**ParisShell** is a C# interactive command-line interface that manages a complete food delivery system between clients and cooks across Paris. The project integrates MySQL database interactions, Excel data imports, graph theory (for metro navigation), and user/role-based authentication. The interface is styled and enhanced using `Spectre.Console`.

## Launching the Shell

To start the CLI interface:

```bash
dotnet run
```

Upon launch, a stylized banner is displayed, and the user is prompted with a shell-like interface:

```bash
anon@paris:mysql:~$
```

## Command Index

| Command        | Description |
|----------------|-------------|
| `help`          | Lists all available commands |
| `tuto`          | Interactive tutorial for using the shell |
| `connect`       | Manual connection to MySQL |
| `autoconnect`   | Auto-login with saved config |
| `login`         | Authenticates a user session |
| `logout`        | Terminates current session |
| `register`      | Registers a new user |
| `initdb`        | Initializes the DB schema and imports Excel data |
| `graph`         | Displays the metro station graph |
| `showtables`    | Lists accessible database tables |
| `showtable`     | Displays data from a table |
| `cinf`          | Displays connection info |
| `clear`         | Clears the terminal screen |
| `exit`          | Exits the shell |

## User Management Commands

```bash
user add                        # Adds a new user to the database
user update <userId>            # Updates a user by ID
user list                       # Lists all users with filters
user getid                      # Retrieves user ID by name/surname
user assign-role <userId>       # Assigns a role to a user
```

These commands are available to users with role `ADMIN` or `BOZO` only.

## Client Commands

```bash
client newc         # Place a new order
client orders       # View past orders
client cancel       # Cancel a pending order
order-travel <id>   # Displays the metro path between cook and client for a given order
```

The `client newc` command uses metro distances to estimate delivery time before placing an order.

## Cook Commands

```bash
cook clients            # Lists all clients who ordered the cook's dishes
cook stats              # Shows statistics for dishes (sales, popularity)
cook platdujour         # Sets or changes the daily special
cook ventes             # Displays earnings per dish and total revenue
```

These commands provide visibility into the cook's performance, customer base, and inventory.

## Analytics Commands

```bash
analytics delivery        # Average delivery time per dish type and area
analytics orders          # Orders per day/week/month
analytics avg-price       # Average price per dish type
analytics avg-acc         # Average accessibility score (client-cook metro distance)
analytics client-orders   # Number of orders per client, sorted by frequency
```

These commands are reserved for `ADMIN` or `BOZO` users only. They query large datasets and return structured tables.

---

## Database Initialization (`initdb`)

### Purpose

`initdb` is a critical command for setting up the entire schema and importing data from Excel files into MySQL.

### Steps Executed

1. **Prompts for MySQL root password**
2. **Drops and recreates the `Livininparis_219` database**
3. **Creates all required tables**:
   - `roles`, `users`, `user_roles`, `clients`
   - `stations_metro`, `connexions_metro`
   - `plats`, `commandes`, `evaluations`
4. **Imports Excel files:**
   - `MetroParis.xlsx` → stations and connections
   - `user.xlsx` → predefined user dataset
   - `plats_simules.xlsx` → mocked dish dataset
5. **Displays Spectre.Console progress bars for feedback**
6. **Closes the connection securely**

Each table includes proper keys, constraints, and foreign key relationships. See `InitDbCommand.cs` for full SQL creation logic.

---

## Graph System

### Overview

The metro is represented using a generic graph structure (`Graph<T>`), where each node is a metro station (`StationData`). The edges are weighted by physical distance (in meters) between connected stations.

### Components

- `GraphLoader`  
  Loads stations and connections from MySQL and builds the `Graph<StationData>` instance.

- `Graph<T>`  
  Core graph logic with:
  - `DijkstraCheminPlusCourt`  
  - `BellmanFordCheminPlusCourt`  
  - `FloydWarshallDistance` (partial)
  - BFS, DFS traversal
  - Methods for checking connectivity, circuits
  - Visualization methods (SVG via SkiaSharp)

- `TempsCheminStations`  
  Calculates the estimated travel time along a path, using metro average speed and transfer buffer.

### Visualization

The graph is visualized using SkiaSharp (SVG output):

- Edges are directional (with arrows)
- Node color and size depend on degree (number of connections)
- Graph is exported to `graph_geo.svg`

### Usage

```bash
graph
```

Also used implicitly in:

- `order-travel`
- `client newc` (to compute ETA before showing dishes)

---

## Security & Sessions

ParisShell uses a session system:

- Users login with email and password (`login`)
- Session state includes role and user info
- Commands check role-based authorization
- Only admins can access analytics and DB admin tools

---

## MySQL Integration

The project interacts with a MySQL database using:

- `MySql.Data.MySqlClient`
- Manual commands and prepared statements
- A singleton `SqlService` to manage connection state
- `Session` to persist current authenticated user

---

## Excel Data Import

Via `OfficeOpenXml`, the following files are parsed and inserted into the DB:

| File                     | Table(s) Populated        |
|--------------------------|---------------------------|
| `MetroParis.xlsx`        | `stations_metro`, `connexions_metro` |
| `user.xlsx`              | `users`, `clients`, `user_roles` |
| `plats_simules.xlsx`     | `plats` |

Custom import services include:

- `ImportStations`
- `Connexions`
- `ImportUser`
- `ImportDishes`

---

## Styling and CLI UX

The project uses `Spectre.Console` to enhance terminal UX through:

- Rich prompts with validation
- Tables, progress bars, spinners
- Colored headers and status indicators
- Contextual success, error, and warning output

---

## Authors

  - **Antoine GROUSSARD** - *Lead Developer and Creator* - [user7845-pm](https://github.com/user7845-pm)
  - **Lucie GRANDET** - *Lead Developer and Creator* - [lvciie](https://github.com/lvciie)
  - **Tristan LIMOUSIN** - *Lead Developer and Creator* - [user6504-pm](https://github.com/user6504-pm)

---

## More informations

- For more information, please see the 'Documentation' file.
