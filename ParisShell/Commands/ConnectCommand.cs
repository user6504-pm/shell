using Spectre.Console;
using ParisShell.Services;
using ParisShell.Models;
using System;
using System.Collections.Generic;

namespace ParisShell.Commands {
    internal class ConnectCommand : ICommand {
        public string Name => "connect";

        public void Execute(string[] args) {
            var arguments = ParseArguments(args);

            if (!arguments.ContainsKey("user") || !arguments.ContainsKey("password")) {
                AnsiConsole.MarkupLine("[red]⛔ Vous devez spécifier un utilisateur avec '-u' et un mot de passe avec '--password'.[/]");
                return;
            }

            string user = arguments["user"];
            string password = arguments["password"];
            string host = arguments.ContainsKey("host") ? arguments["host"] : "localhost"; // Valeur par défaut pour l'hôte
            string database = arguments.ContainsKey("db") ? arguments["db"] : "mysql"; // Valeur par défaut pour la base de données
            string port = arguments.ContainsKey("port") ? arguments["port"] : "3306"; // Valeur par défaut pour le port

            // Utiliser SqlService pour la connexion
            var sqlService = new SqlService();
            var config = new SqlConnectionConfig {
                SERVER = host,
                PORT = port, // Correctement attribuer le port
                UID = user,
                DATABASE = database,
                PASSWORD = password
            };

            sqlService.Connect(config);
        }

        // Méthode pour analyser les arguments de la commande
        private Dictionary<string, string> ParseArguments(string[] args) {
            var result = new Dictionary<string, string>();

            for (int i = 0; i < args.Length; i++) {
                // Identifier et associer les options et leurs valeurs
                if (args[i] == "-u" || args[i] == "--user") {
                    if (i + 1 < args.Length) {
                        result["user"] = args[i + 1];
                        i++; // Passer à l'argument suivant après le `-u`
                    }
                    else {
                        AnsiConsole.MarkupLine("[red]⛔ L'option '-u' requiert un utilisateur après.[/]");
                    }
                }
                else if (args[i] == "--password" || args[i] == "-p") {
                    if (i + 1 < args.Length) {
                        result["password"] = args[i + 1];
                        i++; // Passer à l'argument suivant après le `--password`
                    }
                    else {
                        AnsiConsole.MarkupLine("[red]⛔ L'option '--password' requiert un mot de passe après.[/]");
                    }
                }
                else if (args[i] == "--host" || args[i] == "-h") {
                    if (i + 1 < args.Length) {
                        result["host"] = args[i + 1];
                        i++;
                    }
                    else {
                        AnsiConsole.MarkupLine("[red]⛔ L'option '--host' requiert un nom d'hôte après.[/]");
                    }
                }
                else if (args[i] == "--db") {
                    if (i + 1 < args.Length) {
                        result["db"] = args[i + 1];
                        i++;
                    }
                    else {
                        AnsiConsole.MarkupLine("[red]⛔ L'option '--db' requiert un nom de base de données après.[/]");
                    }
                }
                else if (args[i] == "--port") {
                    if (i + 1 < args.Length) {
                        result["port"] = args[i + 1];
                        i++;
                    }
                    else {
                        AnsiConsole.MarkupLine("[red]⛔ L'option '--port' requiert un numéro de port après.[/]");
                    }
                }
                else {
                    AnsiConsole.MarkupLine($"[yellow]Option non reconnue : {args[i]}[/]");
                }
            }

            return result;
        }
    }
}
