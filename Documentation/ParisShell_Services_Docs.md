# 📘 Technical Documentation: `SqlService`

## Overview

`SqlService` is the central class responsible for managing the connection between the ParisShell CLI and the MySQL database. It abstracts away raw connection logic and exposes high-level methods used throughout the application by commands and user sessions.

---

## 📂 Location
**File**: `Services/SqlService.cs`

---

## 🧩 Responsibility

- Manage connection lifecycle (open/close)
- Provide access to the active connection for executing SQL commands
- Keep track of whether the system is connected

---

## 📌 Key Methods

### ✅ `bool IsConnected`
Returns whether a connection to the MySQL database is currently open.

---

### 🔐 `void Connect(string connectionString)`
```csharp
public void Connect(string connectionString)
```
Opens a new connection using the provided MySQL connection string.

- If a connection already exists, it closes and reopens it.
- Used by:
  - `ConnectCommand`
  - `AutoConnectCommand`
  - `InitDbCommand`

---

### ❌ `void Disconnect()`
```csharp
public void Disconnect()
```
Closes the current connection and updates internal flags.
Used by:
- `DisconnectCommand`
- `Shell` during cleanup

---

### 🔄 `MySqlConnection GetConnection()`
```csharp
public MySqlConnection GetConnection()
```
Returns the currently active connection object. If the connection is null or closed, throws an exception.
Used by:
- All database-accessing commands (e.g. `ShowTablesCommand`, `InitDbCommand`, `ClientCommand`, `GraphLoader`, etc.)

---

## ⚠️ Error Handling

- If the connection fails, exceptions are caught and displayed via `Shell.PrintError`.
- Connection checks (`IsConnected`) prevent runtime errors during execution.

---

## 💡 Typical Usage

```csharp
if (!_sqlService.IsConnected)
    _sqlService.Connect(connectionString);

using var command = new MySqlCommand("SELECT * FROM users", _sqlService.GetConnection());
```

---

# 📘 Technical Documentation: `Session`

## Overview

The `Session` class manages user authentication and role-based context during an active terminal session. It acts as an in-memory state container and provides role-checking logic for authorization.

---

## 📂 Location
**File**: `Services/Session.cs`

---

## 🧩 Responsibility

- Track the currently authenticated user
- Verify role permissions for command access
- Control session-based UI prompts

---

## 📌 Key Properties

### 👤 `Utilisateur CurrentUser`
The user object for the authenticated session, retrieved from the database via `LoginCommand`.

---

### 🔐 `bool IsAuthenticated`
Indicates if a user is currently logged in.

---

## 🔍 Key Methods

### ✅ `void Authenticate(Utilisateur user)`
```csharp
public void Authenticate(Utilisateur user)
```
Sets the current user session with the given `Utilisateur` instance.

Used by:
- `LoginCommand`
- `RegisterCommand` (after successful registration)

---

### ❌ `void Logout()`
```csharp
public void Logout()
```
Clears the current user session.

Used by:
- `LogoutCommand`

---

### 🔐 `bool IsInRole(string role)`
```csharp
public bool IsInRole(string role)
```
Checks if the authenticated user has the specified role (`CLIENT`, `CUISINIER`, `ADMIN`, `BOZO`).

Used by:
- Commands like `CookCommand`, `ClientCommand`, `AnalyticsCommand`

---

## 🛠️ Integration

| Command          | Usage                                     |
|------------------|--------------------------------------------|
| `LoginCommand`   | Authenticates and sets session user        |
| `LogoutCommand`  | Clears session                             |
| `ClientCommand`  | Restricts access to authenticated clients  |
| `CookCommand`    | Restricts to `CUISINIER` role              |
| `AnalyticsCommand` | Restricts to `ADMIN`/`BOZO`               |

---

## 📌 Example

```csharp
if (_session.IsAuthenticated && _session.IsInRole("CUISINIER"))
{
    // Show cooking dashboard
}
else
{
    Shell.PrintError("Access denied.");
}
```

---

## ✅ Summary

`SqlService` and `Session` are foundational service classes enabling the CLI to manage state and data access securely. Their abstraction ensures a clean separation between UI, logic, and data persistence, supporting safe multi-command flows, contextual command restrictions, and shared connection handling.