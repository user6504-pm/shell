using Spectre.Console;
using ParisShell.Services;

namespace ParisShell.Commands {
    internal class TutoCommand : ICommand {
        public string Name => "tuto";


        public void Execute(string[] args) {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[bold underline deeppink4_2]ParisShell Interactive Tutorial[/]");
            AnsiConsole.MarkupLine("[dim]You'll be guided step-by-step. Type the command exactly as shown to move forward.[/]");
            AnsiConsole.MarkupLine("");


            WaitFor("Step 1 - To begin you need to initialize the MySQL database", "Type[green]initdb[/] and press[bold]Enter[/]", expected: "initdb");
            AnsiConsole.MarkupLine("The system will ask you for the MySQL password you use to establish a connection, this is the same password you use to connect to the MySQL Workbench.");
            ConfirmStep("MySQL is initialize.");

            WaitFor("Step 2 - Now you have to connect to MySQL server", "Type [green]connect[/] and press [bold]Enter[/]", expected: "connect");
            AnsiConsole.MarkupLine("Similar to step 1, the system will ask you for your MySql password");
            ConfirmStep("MySQL connection successful!");

            WaitFor("Step 2️ - Log in to your user", "Type [green]login -e <email> -p <password>[/]", "login");


            WaitFor("Step 3️⃣ - List accessible tables", "Type [green]showtables[/]", "showtables");
            ConfirmStep("✅ Tables displayed.");

            WaitFor("Step 4️⃣ - View content of a table", "Example: [green]showtable users[/]", "showtable");
            ConfirmStep("✅ Table content shown.");

            WaitFor("Final Step 🔚 - Exit the shell", "Type [green]exit[/] to leave the program", "exit");
            ConfirmStep("👋 Tutorial complete. You're ready!");

            AnsiConsole.MarkupLine("\n[bold green]Thanks for following the tutorial![/] You can type [blue]help[/] anytime.");
        }

        private void WaitFor(string stepTitle, string instruction, string expected) {
            if (!string.IsNullOrWhiteSpace(stepTitle))
                AnsiConsole.MarkupLine($"\n[bold yellow]{stepTitle}[/]");
            if (!string.IsNullOrWhiteSpace(instruction))
                AnsiConsole.MarkupLine(instruction);

            string input;
            do {
                input = AnsiConsole.Prompt(new TextPrompt<string>("[grey]>[/]").PromptStyle("white")).Trim().ToLower();
            } while (!input.StartsWith(expected));
        }

        private void ConfirmStep(string message) {
            AnsiConsole.MarkupLine($"\n[bold green]{message}[/]");
            AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
            Console.ReadKey(true);
        }
    }
}
