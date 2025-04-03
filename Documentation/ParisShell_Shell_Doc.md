# 📘 Technical Documentation: `Shell`

## Overview

The `Shell` class is the main entry point of the ParisShell application. It serves as the interactive command-line interface that binds all commands, manages session state, and coordinates command dispatch. It provides a REPL-like interface for user interaction with full role-aware command resolution.

---

## 🔍 Location

**File:** `Shell.cs`

---

## 🔧 Responsibilities

- Displays stylized startup banner using `Spectre.Console`
- Manages user session and SQL connection lifecycle
- Maps and dispatches user input to registered commands
- Provides contextual CLI feedback (prompt, errors, warnings)
- Controls access to role-restricted commands

---

## 🧱 Key Properties

```csharp
private readonly Dictionary<string, Action<string[]>> commands;
private readonly SqlService _sqlService;
private readonly Session _session;
```

### `commands`
Holds the mapping between command strings (like `"login"`, `"client"`, etc.) and their associated method invocations.

### `_sqlService`
Instance of `SqlService`, used to handle MySQL connections.

### `_session`
Instance of `Session`, tracks user authentication state and role information.

---

## 🔄 Method: `Run()`

```csharp
public static async Task Run()
```

Main interactive loop of the CLI. Responsibilities:

1. Displays banner and help message
2. Displays a styled prompt like:
   ```bash
   anon@paris:mysql:~$
   ```
3. Parses and dispatches input to the appropriate command
4. Catches and displays runtime errors
5. Terminates on `"exit"` input

Prompt reflects session state:
- Anonymous (`anon`)
- MySQL-only (`mysql`)
- Authenticated (`username`)

---

## 🧠 Helper Methods

### `GetPromptUser()`
Returns the current shell identity: anonymous, SQL, or authenticated user.

### `Statusus()`
Returns a segment of the prompt to show SQL and authentication status:
- `[red]mysql:~[/]` if not connected
- `[orange1]auth:~[/]` if no user authenticated
- `[green]~[/]` if all good

### `PrintError(string)` / `PrintSucces(string)` / `PrintWarning(string)`
Wrappers around `AnsiConsole.MarkupLine()` to standardize CLI output.

---

## ✅ Command Registration

```csharp
commands["login"] = args => new LoginCommand(...).Execute(args);
commands["initdb"] = async args => await new InitDbCommand().Execute(args);
commands["client"] = args => new ClientCommand(...).Execute(args);
...
```

Commands are registered in the constructor of the `Shell` class. Each entry maps a string to a `Command` implementation (synchronous or async).

---

## 💬 Input Parsing

```csharp
string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
string name = parts[0].ToLower();
string[] args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();
```

Supports arbitrary argument parsing via string splitting. Normalizes command name to lowercase for case-insensitivity.

---

## ✅ Summary

The `Shell` class centralizes the REPL logic and acts as the glue layer between the UI/UX, command routing, SQL services, and user session management. It ensures a seamless and styled terminal interface with proper access control, error messaging, and extensibility.