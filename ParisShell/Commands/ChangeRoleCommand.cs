using MySql.Data.MySqlClient;
using ParisShell.Services;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using ParisShell.Models;

namespace ParisShell.Commands
{

    internal class ChangeRoleCommand : ICommand
    {

        private readonly SqlService _sqlService;
        private readonly Session _session;
        public string Name => "changerole";

        public ChangeRoleCommand(SqlService sqlService, Session session)
        {
            _sqlService = sqlService;
            _session = session;
        }
        public void Execute(string[] args)
        {
            if (!_session.IsAuthenticated || !_session.IsInRole("CUISINIER") && !_session.IsInRole("CLIENT"))
            {
                Shell.PrintError("Access restricted to cooks and clients only.");
                return;
            }
            if(_session.IsInRole("CUISINIER"))
            {
                AnsiConsole.WriteLine("Are you sure you want to change your role to CLIENT ?");
                string role = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
          
                    .AddChoices("YES", "NO"));
                if (role == "NO")
                {
                    AnsiConsole.MarkupLine("[yellow]Changement aborted by the user.[/]");
                    return;
                }
                MySqlCommand deletePlatsCmd = new MySqlCommand(
                    "DELETE FROM plats WHERE user_id = @uid",
                    _sqlService.GetConnection());

                deletePlatsCmd.Parameters.AddWithValue("@uid", _session.CurrentUser.Id);
                deletePlatsCmd.ExecuteNonQuery();
                deletePlatsCmd.Dispose();
                
                MySqlCommand getRoleIdCmd = new MySqlCommand(
                    "SELECT role_id FROM roles WHERE role_name = 'CLIENT'",
                    _sqlService.GetConnection());

                int roleId = Convert.ToInt32(getRoleIdCmd.ExecuteScalar());
                getRoleIdCmd.Dispose();

                MySqlCommand updateCmd = new MySqlCommand(
                    "UPDATE user_roles SET role_id = @rid WHERE user_id = @uid",
                    _sqlService.GetConnection());

                updateCmd.Parameters.AddWithValue("@rid", roleId);
                updateCmd.Parameters.AddWithValue("@uid", _session.CurrentUser.Id);

                updateCmd.ExecuteNonQuery();
                updateCmd.Dispose();

                MySqlCommand userCmd = new MySqlCommand(@"
                    SELECT user_id, nom, prenom, email 
                    FROM users 
                    WHERE user_id = @uid", _sqlService.GetConnection());
                userCmd.Parameters.AddWithValue("@uid", _session.CurrentUser.Id);

                MySqlDataReader reader = userCmd.ExecuteReader();
                reader.Read();
                var user = new User
                {
                    Id = reader.GetInt32("user_id"),
                    LastName = reader.GetString("nom"),
                    FirstName = reader.GetString("prenom"),
                    Email = reader.GetString("email")
                };
                reader.Close();
                userCmd.Dispose();

                MySqlCommand roleCmd = new MySqlCommand(@"
                    SELECT r.role_name
                    FROM user_roles ur
                    JOIN roles r ON r.role_id = ur.role_id
                    WHERE ur.user_id = @uid", _sqlService.GetConnection());
                roleCmd.Parameters.AddWithValue("@uid", user.Id);

                MySqlDataReader roleReader = roleCmd.ExecuteReader();
                while (roleReader.Read())
                {
                    user.Roles.Add(roleReader.GetString("role_name"));
                }
                roleReader.Close();
                roleCmd.Dispose();
                _session.CurrentUser = user;
                AnsiConsole.Clear();
                AnsiConsole.MarkupLine("[green]Role successfully changed to CLIENT.[/]");
            }
            else
            {
                AnsiConsole.WriteLine("Are you sure you want to change your role to COOK ?");
                string role = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()

                    .AddChoices("YES", "NO"));
                if (role == "NO")
                {
                    AnsiConsole.MarkupLine("[yellow]Changement aborted by the user.[/]");
                    return;
                }
                MySqlCommand getRoleIdCmd = new MySqlCommand(
                    "SELECT role_id FROM roles WHERE role_name = 'CUISINIER'",
                    _sqlService.GetConnection());

                int roleId = Convert.ToInt32(getRoleIdCmd.ExecuteScalar());
                getRoleIdCmd.Dispose();

                MySqlCommand updateCmd = new MySqlCommand(
                    "UPDATE user_roles SET role_id = @rid WHERE user_id = @uid",
                    _sqlService.GetConnection());

                updateCmd.Parameters.AddWithValue("@rid", roleId);
                updateCmd.Parameters.AddWithValue("@uid", _session.CurrentUser.Id);

                updateCmd.ExecuteNonQuery();
                updateCmd.Dispose();

                MySqlCommand userCmd = new MySqlCommand(@"
                    SELECT user_id, nom, prenom, email 
                    FROM users 
                    WHERE user_id = @uid", _sqlService.GetConnection());
                userCmd.Parameters.AddWithValue("@uid", _session.CurrentUser.Id);

                MySqlDataReader reader = userCmd.ExecuteReader();
                reader.Read();
                var user = new User
                {
                    Id = reader.GetInt32("user_id"),
                    LastName = reader.GetString("nom"),
                    FirstName = reader.GetString("prenom"),
                    Email = reader.GetString("email")
                };
                reader.Close();
                userCmd.Dispose();

                MySqlCommand roleCmd = new MySqlCommand(@"
                    SELECT r.role_name
                    FROM user_roles ur
                    JOIN roles r ON r.role_id = ur.role_id
                    WHERE ur.user_id = @uid", _sqlService.GetConnection());
                roleCmd.Parameters.AddWithValue("@uid", user.Id);

                MySqlDataReader roleReader = roleCmd.ExecuteReader();
                while (roleReader.Read())
                {
                    user.Roles.Add(roleReader.GetString("role_name"));
                }
                roleReader.Close();
                roleCmd.Dispose();
                _session.CurrentUser = user;
                AnsiConsole.Clear();
                AnsiConsole.MarkupLine("[green]Role successfully changed to COOK.[/]");
            }

        }
    }
}
