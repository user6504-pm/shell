using Spectre.Console;
using ParisShell.Services;
using ParisShell.Models;
using System;
using System.Collections.Generic;

namespace ParisShell.Commands {
    internal class ConnectCommand : ICommand {

        private readonly SqlService _sqlService;

        public string Name => "connect";

        public ConnectCommand(SqlService sqlService) {
            _sqlService = sqlService;
        }

        public void Execute(string[] args) {

            if (_sqlService.IsConnected) {
                Shell.PrintWarning("Already connected.");
                return;
            }

            var arguments = ParseArguments(args);

            if (!arguments.ContainsKey("user") || !arguments.ContainsKey("password")) {
                Shell.PrintError("You must provide a user with '-u' and a password with '--password'.");
                return;
            }

            string user = arguments["user"];
            string password = arguments["password"];
            string host = arguments.ContainsKey("host") ? arguments["host"] : "localhost";
            string database = arguments.ContainsKey("db") ? arguments["db"] : "Livininparis_219";
            string port = arguments.ContainsKey("port") ? arguments["port"] : "3306";

            var config = new SqlConnectionConfig {
                SERVER = host,
                PORT = port,
                UID = user,
                DATABASE = database,
                PASSWORD = password
            };

            _sqlService.Connect(config);
        }

        private Dictionary<string, string> ParseArguments(string[] args) {
            var result = new Dictionary<string, string>();

            for (int i = 0; i < args.Length; i++) {
                if (args[i] == "-u" || args[i] == "--user") {
                    if (i + 1 < args.Length) {
                        result["user"] = args[++i];
                    }
                    else {
                        Shell.PrintError("'-u' requires a user.");
                    }
                }
                else if (args[i] == "--password" || args[i] == "-p") {
                    if (i + 1 < args.Length) {
                        result["password"] = args[++i];
                    }
                    else {
                        Shell.PrintError("'--password' requires a value.");
                    }
                }
                else if (args[i] == "--host" || args[i] == "-h") {
                    if (i + 1 < args.Length) {
                        result["host"] = args[++i];
                    }
                    else {
                        Shell.PrintError("'--host' requires a value.");
                    }
                }
                else if (args[i] == "--db") {
                    if (i + 1 < args.Length) {
                        result["db"] = args[++i];
                    }
                    else {
                        Shell.PrintError("'--db' requires a value.");
                    }
                }
                else if (args[i] == "--port") {
                    if (i + 1 < args.Length) {
                        result["port"] = args[++i];
                    }
                    else {
                        Shell.PrintError("'--port' requires a value.");
                    }
                }
                else {
                    Shell.PrintWarning($"Unknown option: {args[i]}");
                }
            }

            return result;
        }
    }
}
