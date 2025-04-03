# ParisShell System Architecture Documentation

## Overview

**ParisShell** is a modular, extensible, and role-based interactive command-line interface built in C#. The system is designed to simulate a food delivery infrastructure in the context of the Paris metro system, leveraging a MySQL database backend, Excel data imports, and dynamic graph traversal algorithms for delivery estimations. The shell is both developer-friendly and interactive thanks to its extensive use of `Spectre.Console`.

---

## Key Features

- Modular command architecture using `ICommand`
- Graph-based routing over Paris metro network
- Dynamic database initialization and population via `initdb`
- Role-based access control (Client, Cuisinier, Admin, Bozo)
- Integration with MySQL and Excel for persistence and data imports
- Styled CLI UX with visual feedback (spinners, tables, prompts)
- Exportable SVG metro graphs with SkiaSharp

---

## Architectural Layers

### 1. Entry Point

- **Program.cs**: Initializes the `Shell` class and enters the main loop via `Shell.Run()`.

### 2. Command Dispatcher

- **Shell.cs**: Central component that holds a dictionary of registered commands (as `string` => `Action<string[]>` or `Func<Task>`).
  - Handles user authentication and current session state
  - Dispatches parsed input to correct command handler

### 3. Command Classes

- All commands implement `ICommand`
- Located in `Commands/` directory
- Examples:
  - `InitDbCommand.cs`: Initializes database and imports Excel data
  - `ClientCommand.cs`: Client interactions like `newc`, `orders`, `cancel`
  - `CookCommand.cs`: For cuisiniers to view stats, manage plats
  - `AnalyticsCommand.cs`: Administrative access to global data metrics

### 4. Services

Encapsulate common logic reused across commands.

| Service Class       | Responsibility |
|---------------------|----------------|
| `SqlService`        | Manages and shares the active MySQL connection |
| `Session`           | Holds currently authenticated user & role state |
| `ImportStations`    | Imports station metadata from Excel |
| `ImportUser`        | Imports users and assigns roles/stations |
| `ImportDishes`      | Imports dishes (plats) |
| `Connexions`        | Reads and inserts metro station connections |

All services interact with the database using raw `MySqlCommand` and connection from `SqlService`.

### 5. Models

Defines structures used across commands and services.

| Model            | Description |
|------------------|-------------|
| `Utilisateur`    | Represents a user, used for authentication/session |
| `StationData`    | Holds data per metro station for graph processing |
| `Noeud<T>`       | Node abstraction for graphs |
| `Lien<T>`        | Weighted edge abstraction for graphs |

### 6. Graph Engine

Located in `Graph/`. Custom generic graph implementation.

- `Graph<T>`: Core graph class (undirected, weighted)
  - Supports:
    - BFS / DFS
    - Dijkstra / Bellman-Ford / Floyd-Warshall
    - Graph export to SVG (geographical layout)
- `GraphLoader`: Builds a metro network graph from DB
  - Uses `StationData` and `connexions_metro`

### 7. Data Import & Initialization

- `InitDbCommand.cs`:
  - Drops and recreates schema `Livininparis_219`
  - Sequentially executes SQL scripts for table creation
  - Imports data from:
    - `MetroParis.xlsx`: Stations & connections
    - `user.xlsx`: Clients
    - `plats_simules.xlsx`: Dishes (plats)
  - Displays dynamic progress using `AnsiConsole.Progress`

### 8. SkiaSharp Integration

- Graph export is handled by `Graph<T>.ExporterSvg()`
- Uses `SKSvgCanvas` to render colored nodes and directional arrows
- Nodes' radius and color reflect degree (connections)
- Special styling for path nodes (highlighted in red)

---

## Authentication & Roles

- Managed via `Session.cs`
- Supported Roles:
  - `CLIENT`
  - `CUISINIER`
  - `ADMIN`
  - `BOZO` (Super admin)
- Affects which commands and tables are accessible

---

## Command Help Structure

```bash
> help

user add
client newc
analytics delivery
initdb
graph
...
```

Each command follows a naming convention and structure (e.g., `client`, `cook`, `analytics`) to ensure clear separation of domain logic.

---

## Data Flow

1. **DB Bootstrap**: `initdb` imports all static and dynamic data
2. **Login/Register**: User authenticates via `login` or `register`
3. **Role Dispatch**: Based on role, access to commands/tables is granted
4. **Command Execution**: Shell dispatches command via delegate map
5. **Database IO**: Services use SQL to read/write data
6. **Graph Analysis**: Orders are analyzed via Dijkstra or Bellman-Ford
7. **Feedback & UI**: CLI uses Spectre.Console for visual clarity

---

## Error Handling

- Centralized output via `Shell.PrintError`, `PrintSucces`, `PrintWarning`
- All `Execute` methods wrap database interaction with try/catch
- Interactive prompts prevent most bad input before execution

---

## Extensibility

New commands can be added easily:

```csharp
public class MyCommand : ICommand {
    public string Name => "mycmd";
    public void Execute(string[] args) {
        // Your logic
    }
}
```

And register it in `Shell.cs`:
```csharp
commands["mycmd"] = args => new MyCommand().Execute(args);
```

---

## Summary

ParisShell is a fully modular, database-connected, and graph-integrated CLI application. It’s highly structured with separation between user interface (Shell), business logic (Commands), and data access (Services). It is ideal for educational purposes, demo environments, or production-facing admin tools.