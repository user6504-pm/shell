using Spectre.Console;
using ParisShell.Services;
using ParisShell.Models;
using MySql.Data.MySqlClient;
using System.Data;

namespace ParisShell.Commands;

internal class UserCommand : ICommand {
    public string Name => "user";

    private readonly SqlService _sqlService;
    private readonly Session _session;

    private string Ask(string label) => AnsiConsole.Ask<string>($"[blue]{label} :[/]");
    private string AskSecret(string label) => AnsiConsole.Prompt(new TextPrompt<string>($"[red]{label} :[/]").Secret());
    public UserCommand(SqlService sqlService, Session session) {
        _sqlService = sqlService;
        _session = session;
    }

    public void Execute(string[] args) {
        if (!_session.IsInRole("ADMIN") && !_session.IsInRole("BOZO")) {
            Shell.PrintError("Access denied. Admin or bozo only.");
            return;
        }

        if (args.Length == 0) {
            Shell.PrintWarning("Usage: user [add|update|assign-role|list]");
            return;
        }

        switch (args[0]) {
            case "add":
                AddUser();
                break;
            case "assign-role":
                if (args.Length < 2 || !int.TryParse(args[1], out int assignId)) {
                    Shell.PrintError("Usage: user assign-role <userId>");
                    return;
                }
                AssignRole(assignId);
                break;
            case "update":
                if (args.Length < 2 || !int.TryParse(args[1], out int updateId)) {
                    Shell.PrintError("Usage: user update <userId>");
                    return;
                }
                UpdateUser(updateId);
                break;
            case "list":
                ListUsers();
                break;
            case "getid":
                GetUserId();
                break;
            default:
                Shell.PrintError("Unknown subcommand.");
                break;
        }
    }

    private void AddUser() {
        var firstname = Ask("First name");
        var lastname = Ask("Last name");
        var adress = Ask("Address");
        var phone = Ask("Phone");
        var email = Ask("Email");
        var pwd = AskSecret("Password");
        var subway = AnsiConsole.Ask<int>("Closest metro station ID:");

        int userId;
        using (var cmd = new MySqlCommand("INSERT INTO users (nom, prenom, adresse, telephone, email, mdp, metroproche) VALUES (@n, @p, @a, @t, @e, @m, @mp); SELECT LAST_INSERT_ID();", _sqlService.GetConnection())) {
            cmd.Parameters.AddWithValue("@n", firstname);
            cmd.Parameters.AddWithValue("@p", lastname);
            cmd.Parameters.AddWithValue("@a", adress);
            cmd.Parameters.AddWithValue("@t", phone);
            cmd.Parameters.AddWithValue("@e", email);
            cmd.Parameters.AddWithValue("@m", pwd);
            cmd.Parameters.AddWithValue("@mp", subway);
            userId = Convert.ToInt32(cmd.ExecuteScalar());
        }

        string role = SelectRole();
        InsertUserRole(userId, role);
        Shell.PrintSucces($"User {firstname} {lastname} successfully created with role '{role}'.");

        if (role.ToLower() == "client")
            AddClient(userId);
    }

    private void UpdateUser(int id)
    {
        if (!UserExists(id))
        {
            Shell.PrintError($"No user found with ID {id}.");
            return;
        }

        var firstname = Ask("New first name");
        var lastname = Ask("New last name");
        var adress = Ask("New address");
        var tel = Ask("Phone");
        var pwd = AskSecret("New password");

        using var cmd = new MySqlCommand(@"
        UPDATE users SET nom=@n, prenom=@p, adresse=@a, telephone=@t, mdp=@m WHERE user_id=@id", _sqlService.GetConnection());
        cmd.Parameters.AddWithValue("@n", lastname);
        cmd.Parameters.AddWithValue("@p", firstname);
        cmd.Parameters.AddWithValue("@a", adress);
        cmd.Parameters.AddWithValue("@t", tel);
        cmd.Parameters.AddWithValue("@m", pwd);
        cmd.Parameters.AddWithValue("@id", id);

        int rows = cmd.ExecuteNonQuery();
        if (rows > 0)
            Shell.PrintSucces("User successfully updated.");
        else
            Shell.PrintWarning("No update performed.");
    }

    private void AssignRole(int userId)
    {
        if (!UserExists(userId))
        {
            Shell.PrintError($"No user found with ID {userId}.");
            return;
        }

        string role = SelectRole();
        InsertUserRole(userId, role);
        Shell.PrintSucces($"Role '{role}' assigned to user {userId}.");
        if (role.ToLower() == "client")
            AddClient(userId);
    }

    private void ListUsers()
    {
        string sort = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Sort by:")
                .AddChoices("Name (A-Z)", "Address", "Total purchases"));

        string query;

        if (sort == "Total purchases")
        {
            query = @"
        SELECT u.user_id AS 'ID',
               u.nom AS 'Last Name',
               u.prenom AS 'First Name',
               u.adresse AS 'Address',
               u.email AS 'Email',
               CONCAT(IFNULL(SUM(p.prix_par_personne * c.quantite), 0)) AS 'Total Amount'
        FROM users u
        LEFT JOIN commandes c ON u.user_id = c.client_id
        LEFT JOIN plats p ON c.plat_id = p.plat_id
        GROUP BY u.user_id, u.nom, u.prenom, u.adresse, u.email
        ORDER BY SUM(p.prix_par_personne * c.quantite) DESC";
        }
        else
        {
            string orderColumn = sort == "Address" ? "u.adresse" : "u.nom";
            query = $@"
        SELECT u.user_id AS 'ID',
               u.nom AS 'Last Name',
               u.prenom AS 'First Name',
               u.adresse AS 'Address',
               u.email AS 'Email',
               CONCAT(IFNULL((
                   SELECT SUM(p.prix_par_personne * c.quantite)
                   FROM commandes c
                   JOIN plats p ON c.plat_id = p.plat_id
                   WHERE c.client_id = u.user_id
               ), 0), '€') AS 'Total Amount'
        FROM users u
        ORDER BY {orderColumn} ASC";
        }

        _sqlService.ExecuteAndDisplay(query);
    }

    private string SelectRole() {
        var roles = new List<string>();
        using var cmd = new MySqlCommand("SELECT role_name FROM roles", _sqlService.GetConnection());
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            roles.Add(reader.GetString(0));

        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a role:")
                .AddChoices(roles));
    }

    private void InsertUserRole(int userId, string role) {
        using var getRole = new MySqlCommand("SELECT role_id FROM roles WHERE role_name = @r", _sqlService.GetConnection());
        getRole.Parameters.AddWithValue("@r", role);
        var roleId = Convert.ToInt32(getRole.ExecuteScalar());

        using var insert = new MySqlCommand("INSERT INTO user_roles (user_id, role_id) VALUES (@u, @r)", _sqlService.GetConnection());
        insert.Parameters.AddWithValue("@u", userId);
        insert.Parameters.AddWithValue("@r", roleId);
        insert.ExecuteNonQuery();
    }

    private void AddClient(int userId) {
        string type = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Client type:")
                .AddChoices("PARTICULIER", "ENTREPRISE"));

        using var cmd = new MySqlCommand("INSERT INTO clients (client_id, type_client) VALUES (@id, @type)", _sqlService.GetConnection());
        cmd.Parameters.AddWithValue("@id", userId);
        cmd.Parameters.AddWithValue("@type", type);
        cmd.ExecuteNonQuery();
    }
    private void GetUserId()
    {
        var lastname = Ask("Last name");
        var firstname = Ask("First name");

        string query = @"
    SELECT user_id AS 'User ID', email AS 'Email'
    FROM users
    WHERE nom LIKE @n AND prenom LIKE @p";

        using var cmd = new MySqlCommand(query, _sqlService.GetConnection());
        cmd.Parameters.AddWithValue("@n", $"%{lastname}%");
        cmd.Parameters.AddWithValue("@p", $"%{firstname}%");

        using var reader = cmd.ExecuteReader();

        var results = new List<(string Id, string Email)>();
        while (reader.Read())
        {
            results.Add((reader["User ID"].ToString(), reader["Email"].ToString()));
        }

        reader.Close();

        if (results.Count == 0)
        {
            Shell.PrintWarning("No user found.");
        }
        else if (results.Count > 10)
        {
            Shell.PrintWarning("Too many results. Please refine your search.");
        }
        else
        {
            _sqlService.ExecuteAndDisplay(query, new Dictionary<string, object> {
            { "@n", $"%{lastname}%" },
            { "@p", $"%{firstname}%" }
        });
        }
    }

    private bool UserExists(int userId)
    {
        using var cmd = new MySqlCommand("SELECT COUNT(*) FROM users WHERE user_id = @id", _sqlService.GetConnection());
        cmd.Parameters.AddWithValue("@id", userId);
        var result = Convert.ToInt32(cmd.ExecuteScalar());
        return result > 0;
    }
}
