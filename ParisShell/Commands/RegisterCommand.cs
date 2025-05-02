using Spectre.Console;
using MySql.Data.MySqlClient;
using ParisShell.Models;
using ParisShell.Services;
using MimeKit;
using MailKit.Net.Smtp;
using System.Net.Mail;
using System.Net;

namespace ParisShell.Commands
{

    /// <summary>
    /// Command that allows a new user or company to register.
    /// Handles role assignment and client type configuration.
    /// </summary>
    internal class RegisterCommand : ICommand
    {

        /// <summary>
        /// Command name used in the shell.
        /// </summary>
        public string Name => "register";

        private readonly SqlService _sqlService;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterCommand"/> class.
        /// </summary>
        /// <param name="sqlService">MySQL connection service.</param>
        public RegisterCommand(SqlService sqlService)
        {
            _sqlService = sqlService;
        }

        /// <summary>
        /// Executes the registration logic.
        /// Supports both individual and company registration with validation checks.
        /// </summary>
        public void Execute(string[] args)
        {
            if (!_sqlService.IsConnected)
            {
                Shell.PrintError("You must be connected to MySQL first.");
                return;
            }

            string type = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Client type:")
                    .AddChoices("PARTICULIER", "ENTREPRISE"));

            if (type != "ENTREPRISE")
            {
                string role = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select your [red]role[/]:")
                        .AddChoices("CUISINIER", "CLIENT"));

                AnsiConsole.MarkupLine("[bold underline deeppink4_2]User Registration[/]");

                string firstname;
                do
                {
                    firstname = AnsiConsole.Ask<string>("First name:");
                    if (string.IsNullOrWhiteSpace(firstname) || !firstname.All(char.IsLetter))
                        Shell.PrintWarning("First name must only contain letters.");
                } while (string.IsNullOrWhiteSpace(firstname) || !firstname.All(char.IsLetter));

                string lastname;
                do
                {
                    lastname = AnsiConsole.Ask<string>("Last name:");
                    if (string.IsNullOrWhiteSpace(lastname) || !lastname.All(char.IsLetter))
                        Shell.PrintWarning("Last name must only contain letters.");
                } while (string.IsNullOrWhiteSpace(lastname) || !lastname.All(char.IsLetter));

                string address;
                do
                {
                    address = AnsiConsole.Ask<string>("Address:");
                    if (!address.Any(char.IsDigit) || address.Trim().Split(' ').Length < 2)
                        Shell.PrintWarning("Address must contain a street number and a street name.");
                } while (!address.Any(char.IsDigit) || address.Trim().Split(' ').Length < 2);

                string phone;
                do
                {
                    phone = AnsiConsole.Ask<string>("Phone:");
                    if (phone.Length != 10 || !phone.All(char.IsDigit) || !phone.StartsWith("0"))
                        Shell.PrintWarning("Phone number must be exactly 10 digits and start with '0'.");
                } while (phone.Length != 10 || !phone.All(char.IsDigit) || !phone.StartsWith("0"));

                string email;
                do
                {
                    email = AnsiConsole.Ask<string>("Email:");
                    if (!email.Contains("@") || !email.Contains("."))
                        Shell.PrintWarning("Invalid email format.");
                } while (!email.Contains("@") || !email.Contains("."));

                string pwd;
                do
                {
                    pwd = AnsiConsole.Prompt(new TextPrompt<string>("Password:")
                        .PromptStyle("red").Secret(' '));
                    if (pwd.Length < 6)
                        Shell.PrintWarning("Password must be at least 6 characters.");
                } while (pwd.Length < 6);

                int metro = AnsiConsole.Ask<int>("Closest metro station ID:");
                if (!VerifyEmailCode(email, firstname))
                    return;


                int userId;
                using (var cmd = new MySqlCommand(
                    "INSERT INTO users (nom, prenom, adresse, telephone, email, mdp, metroproche) VALUES (@n, @p, @a, @t, @e, @m, @mp); SELECT LAST_INSERT_ID();",
                    _sqlService.GetConnection()))
                {
                    cmd.Parameters.AddWithValue("@n", firstname);
                    cmd.Parameters.AddWithValue("@p", lastname);
                    cmd.Parameters.AddWithValue("@a", address);
                    cmd.Parameters.AddWithValue("@t", phone);
                    cmd.Parameters.AddWithValue("@e", email);
                    cmd.Parameters.AddWithValue("@m", pwd);
                    cmd.Parameters.AddWithValue("@mp", metro);
                    userId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                Shell.PrintSucces("Registration successful! You can now use [green]login[/] to log in.");
                AssignRole(userId, role);
                AddClientDetails(userId, type);
            }
            else
            {
                AnsiConsole.MarkupLine("[bold underline deeppink4_2]Company Registration[/]");

                string companyname;
                do
                {
                    companyname = AnsiConsole.Ask<string>("Company name:");
                    if (string.IsNullOrWhiteSpace(companyname) || !companyname.All(char.IsLetter))
                        Shell.PrintWarning("Company name must only contain letters.");
                } while (string.IsNullOrWhiteSpace(companyname) || !companyname.All(char.IsLetter));

                string codesiren;
                do
                {
                    codesiren = AnsiConsole.Ask<string>("Siren number:");
                    string cleaned = codesiren.Replace(" ", "");

                    if (cleaned.Length != 14 || !cleaned.All(char.IsDigit))
                        Shell.PrintWarning("SIREN must be exactly 14 digits (spaces are ignored).");

                } while (codesiren.Replace(" ", "").Length != 14 || !codesiren.Replace(" ", "").All(char.IsDigit));


                string address;
                do
                {
                    address = AnsiConsole.Ask<string>("Address:");
                    if (!address.Any(char.IsDigit) || address.Trim().Split(' ').Length < 2)
                        Shell.PrintWarning("Address must contain a street number and a street name.");
                } while (!address.Any(char.IsDigit) || address.Trim().Split(' ').Length < 2);

                string phone;
                do
                {
                    phone = AnsiConsole.Ask<string>("Phone:");
                    string cleaned = phone.Replace(" ", "");

                    if (cleaned.Length != 10 || !cleaned.All(char.IsDigit) || !cleaned.StartsWith("0"))
                        Shell.PrintWarning("Phone number must be exactly 10 digits and start with '0' (spaces are ignored).");

                } while (phone.Replace(" ", "").Length != 10 ||
                         !phone.Replace(" ", "").All(char.IsDigit) ||
                         !phone.Replace(" ", "").StartsWith("0"));


                string email;
                do
                {
                    email = AnsiConsole.Ask<string>("Email:");
                    if (!email.Contains("@") || !email.Contains("."))
                        Shell.PrintWarning("Invalid email format.");
                } while (!email.Contains("@") || !email.Contains("."));

                string pwd;
                do
                {
                    pwd = AnsiConsole.Prompt(new TextPrompt<string>("Password:")
                        .PromptStyle("red").Secret(' '));
                    if (pwd.Length < 6)
                        Shell.PrintWarning("Password must be at least 6 characters.");
                } while (pwd.Length < 6);

                int metro = AnsiConsole.Ask<int>("Closest metro station ID:");
                if (!VerifyEmailCode(email, companyname))
                {

                    return;
                }
                int userId;
                using (var cmd = new MySqlCommand(
                    "INSERT INTO users (nom, prenom, adresse, telephone, email, mdp, metroproche) VALUES (@n, @p, @a, @t, @e, @m, @mp); SELECT LAST_INSERT_ID();",
                    _sqlService.GetConnection()))
                {
                    cmd.Parameters.AddWithValue("@n", codesiren);
                    cmd.Parameters.AddWithValue("@p", companyname);
                    cmd.Parameters.AddWithValue("@a", address);
                    cmd.Parameters.AddWithValue("@t", phone);
                    cmd.Parameters.AddWithValue("@e", email);
                    cmd.Parameters.AddWithValue("@m", pwd);
                    cmd.Parameters.AddWithValue("@mp", metro);
                    userId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                Shell.PrintSucces("Registration successful! You can now use [green]login[/] to log in.");
                AssignRole(userId, "CLIENT");
                AddClientDetails(userId, type);
            }
        }

        /// <summary>
        /// Assigns a role to a newly registered user.
        /// </summary>
        private void AssignRole(int userId, string role)
        {
            using var roleCmd = new MySqlCommand("SELECT role_id FROM roles WHERE role_name = @r", _sqlService.GetConnection());
            roleCmd.Parameters.AddWithValue("@r", role);
            int roleId = Convert.ToInt32(roleCmd.ExecuteScalar());

            using var insert = new MySqlCommand("INSERT INTO user_roles (user_id, role_id) VALUES (@u, @r)", _sqlService.GetConnection());
            insert.Parameters.AddWithValue("@u", userId);
            insert.Parameters.AddWithValue("@r", roleId);
            insert.ExecuteNonQuery();
        }

        /// <summary>
        /// Inserts client-specific information into the clients table.
        /// </summary>
        private void AddClientDetails(int userId, string type)
        {
            using var cmd = new MySqlCommand("INSERT INTO clients (client_id, type_client) VALUES (@id, @type)", _sqlService.GetConnection());
            cmd.Parameters.AddWithValue("@id", userId);
            cmd.Parameters.AddWithValue("@type", type);
            cmd.ExecuteNonQuery();
        }
        private bool VerifyEmailCode(string email, string name)
        {
            int code = new Random().Next(100000, 999999);
            int attempts = 0;

            var smtpClient = new System.Net.Mail.SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("parislivin641@gmail.com", "pndk xhjq oqhp czsw"), // Mot de passe d'application Gmail
                EnableSsl = true,
            };

            var mail = new MailMessage
            {
                From = new MailAddress("parislivin641@gmail.com", "LivInParis Verification"),
                Subject = "Your LivInParis verification code",
                IsBodyHtml = true
            };

            mail.To.Add(email);

            mail.Body = $@"
    <html>
    <body style='font-family:Arial,sans-serif;background-color:#f4f4f4;padding:30px;'>
      <div style='max-width:600px;margin:auto;background:#fff;padding:20px;border-radius:8px;border:1px solid #ddd;'>
        <h2 style='color:#5f005f;'>Welcome to LivInParis!</h2>
        <p>Hello <strong>{name}</strong>,</p>
        <p>Thank you for registering. Here is your verification code:</p>
        <div style='font-size:28px;font-weight:bold;color:#333;padding:15px 0;text-align:center;background:#f0f0f0;border-radius:5px;'>{code}</div>
        <p style='margin-top:20px;'>Please enter this code in the console to complete your registration.</p>
        <p style='font-size:14px;color:#888;'>If you did not request this, you can safely ignore this message.</p>
        <br/>
        <p style='font-size:14px;color:#aaa;'>– The LivInParis Team</p>
      </div>
    </body>
    </html>";

            try
            {
                smtpClient.Send(mail);
                AnsiConsole.MarkupLine("[green]A verification code has been sent to your email.[/]");
            }
            catch (Exception ex)
            {
                Shell.PrintError($"[red]Failed to send email: {ex.Message}[/]");
                return false;
            }

            while (attempts < 3)
            {
                int userCode = AnsiConsole.Ask<int>("Enter the 6-digit code you received by email:");
                if (userCode == code)
                {
                    AnsiConsole.MarkupLine("[green]Email verified successfully![/]");
                    return true;
                }

                attempts++;
                if (attempts < 3)
                    AnsiConsole.MarkupLine("[red]Incorrect code. Please try again.[/]");
            }

            Shell.PrintError("Too many failed attempts. Verification failed.");
            return false;
        }

    }
}
