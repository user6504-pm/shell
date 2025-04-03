# 📘 Technical Documentation: `ICommand`

## Overview
`ICommand` is a base interface used in the **ParisShell** architecture to define the contract for all shell commands. It enforces consistency and allows the shell to register and invoke commands dynamically based on user input.

---

## 📍 Location  
**Namespace:** `ParisShell.Commands`  
**File:** Typically declared in `Commands/ICommand.cs`

---

## 🔧 Interface Definition
```csharp
public interface ICommand
{
    string Name { get; }
    void Execute(string[] args);
}
```

---

## ✨ Properties

| Property | Type   | Description                              |
|----------|--------|------------------------------------------|
| `Name`   | string | The keyword that identifies the command. |

Each implementing class defines its own `Name`, which is registered in the `Shell` dispatcher and matched against user input.

---

## 🚀 Method

| Method         | Return | Description                                    |
|----------------|--------|------------------------------------------------|
| `Execute(args)` | void   | Runs the command logic using CLI arguments.   |

All shell commands (e.g., `login`, `register`, `initdb`, `client`) implement this method to encapsulate their functionality.

---

## 🧩 Usage Example

```csharp
internal class HelpCommand : ICommand
{
    public string Name => "help";

    public void Execute(string[] args)
    {
        // Display available commands
    }
}
```

In the `Shell` constructor:

```csharp
commands["help"] = args => new HelpCommand().Execute(args);
```

---

## 🧠 Design Purpose

- Promotes **polymorphism**: All commands can be invoked using the same interface.
- Enables dynamic **command registration** in the shell prompt.
- Simplifies integration and testing of individual command units.

---

## ✅ Summary

The `ICommand` interface is a lightweight but critical part of the command-dispatch system within `ParisShell`, ensuring structure, flexibility, and extensibility for shell command development.