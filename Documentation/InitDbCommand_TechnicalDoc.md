
# 📘 Technical Documentation: `InitDbCommand`

## Overview
`InitDbCommand` is a core administrative command in the ParisShell project responsible for initializing the database schema and seeding it with essential data from Excel files. This is typically used as a bootstrap or reset command by developers or system administrators.

---

## 🚀 Command: `initdb`

### Location
File: `Commands/InitDbCommand.cs`

### Interface Implemented
- `ICommand`

### Entry Method
```csharp
public void Execute(string[] args)
```

---

## 🔧 Functional Flow

### 1. Prompt for MySQL Credentials
```csharp
string mdp = AnsiConsole.Prompt(new TextPrompt<string>("MySQL password..."));
```
Prompts the user to enter the root password for MySQL. The password is masked.

---

### 2. Database Connection
```csharp
maConnexion = new MySqlConnection("..."); 
maConnexion.Open();
```
Establishes a MySQL connection to the local `sys` database with the provided credentials.

---

### 3. Database & Table Creation
A list of SQL statements is sequentially executed to:
- Drop existing `Livininparis_219` database (if any)
- Create new database and switch context to it
- Create the following tables:

#### Tables Created
- `roles`
- `stations_metro`
- `users`
- `user_roles`
- `clients`
- `plats`
- `commandes`
- `evaluations`
- `connexions_metro`

Each table uses appropriate constraints such as `FOREIGN KEY`, `ENUM`, and `CHECK`.

---

### 4. Data Import Tasks

Using Spectre.Console progress indicators, the command imports data using the following static classes:

#### 📄 ImportStations (from `MetroParis.xlsx`)
File: `Services/ImportStations.cs`
- Parses metro station data (name, line, coordinates)
- Populates `stations_metro` table

#### 🔗 Connexions (from `MetroParis.xlsx`)
File: `Services/Connexions.cs`
- Reads pairwise station connections and distances
- Inserts rows into `connexions_metro`

#### 👥 ImportUser (from `user.xlsx`)
File: `Services/ImportUser.cs`
- Imports user data
- Populates `users`, `clients`, and links to `stations_metro`

#### 🍽️ ImportDishes (from `plats_simules.xlsx`)
File: `Services/ImportDishes.cs`
- Populates `plats` table with simulated dishes
- Validates foreign key constraints with `users`

---

## 🛠️ Error Handling

### Try-Catch Blocks
- Handles `MySqlException` during connection and creation
- Catches all exceptions during data import and prints error messages with `Shell.PrintError(...)`

---

## 📊 User Feedback

### Spectre.Console Usage
- `AnsiConsole.Prompt`: Secure password prompt
- `AnsiConsole.Progress`: Dynamic progress tracking for each import phase
- `Shell.PrintSucces`, `Shell.PrintWarning`, `Shell.PrintError`: Custom styled CLI output

---

## 🔗 Dependencies

| Class           | Responsibility                        |
|----------------|----------------------------------------|
| `ImportStations` | Parse and import station metadata     |
| `Connexions`     | Link station pairs with distance data |
| `ImportUser`     | Import users and associate with stations |
| `ImportDishes`   | Import available dishes and metadata  |

---

## ✅ Summary

This class is essential for initializing and populating the system with foundational data. It ensures relational integrity and prepares the system for interaction by other roles (`CLIENT`, `CUISINIER`, etc.). It should be run **once** during setup or whenever a reset is needed.

