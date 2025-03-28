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


            WaitFor("Step 1 - To begin you need to initialize the MySQL database", "Type[green] {initdb}[/] and press[bold] Enter[/]", expected: "initdb");
            AnsiConsole.MarkupLine("The system will ask you for the MySQL password you use to establish a connection, this is the same password you use to connect to MySQL Workbench.");
            AnsiConsole.MarkupLine("Once initialized for the first time, you can use this command when you want to refresh your database. You don't necessary have to use initdb each time you start the code, just once for the first setup and to refresh if there is any bugs.");
            ConfirmStep("MySQL is initialize.");

            WaitFor("Step 2 - Now you have to connect to MySQL server", "Type [green]{connect}[/] and press [bold]Enter[/]", expected: "connect");
            AnsiConsole.MarkupLine("Similar to step 1, the system will ask you for your MySql password.");
            ConfirmStep("MySQL connection successful!");

            WaitFor("Step 3 - Log in to your user", "Type [green]{login}[/] and press [bold]Enter[/]", expected: "login");
            AnsiConsole.MarkupLine("You will be prompted to enter your [blue]email[/] and [red]password[/].");
            AnsiConsole.MarkupLine("Make sure your user is already in the database.");
            AnsiConsole.MarkupLine("If your credentials are valid, your user session will be active and you'll see your roles.");
            ConfirmStep("Logged in successfully!");


            WaitFor("Step 4 - Explore the tables you can access", "Type [green]showtables[/] and press [bold]Enter[/]", expected: "showtables");
            AnsiConsole.MarkupLine("This command shows all the tables you are allowed to see based on your role (e.g. [bold]client[/], [bold]cuisinier[/], [bold]admin[/]).");
            AnsiConsole.MarkupLine("If you're an admin or bozo, you’ll see the full database. Otherwise, you’ll only see a restricted view.");
            ConfirmStep("Visible tables displayed successfully!");


            WaitFor("Step 5 - Learn what you can do", "A command [green]help[/] can be done. It will list all commands available for your current role.", expected: "help");
            AnsiConsole.MarkupLine("For example, an [bold]admin[/] can manage users, roles, analytics, and database info.");
            AnsiConsole.MarkupLine("A [bold]cuisinier[/] can see dishes, orders, stats, and sales.");
            AnsiConsole.MarkupLine("A [bold]client[/] can see their orders and evaluate dishes.");
            AnsiConsole.MarkupLine("");
            AnsiConsole.MarkupLine("This [green]help[/] command only work [bold]after you're logged in[/] to determine your role.");
            ConfirmStep("You're ready to use [green]help[/] as soon as login in the real program.");


            ConfirmStep("\n[bold green]Thanks for following the tutorial![/] You can type [blue]help[/] anytime.");
            AnsiConsole.Clear();
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
