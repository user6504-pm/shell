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

    public UserCommand(SqlService sqlService, Session session) {
        _sqlService = sqlService;
        _session = session;
    }

    public void Execute(string[] args) {
        if (!_session.IsInRole("admin") && !_session.IsInRole("bozo")) {
            AnsiConsole.MarkupLine("[red]⛔ Accès refusé. Réservé à admin ou bozo.[/]");
            return;
        }

        if (args.Length == 0) {
            AnsiConsole.MarkupLine("[yellow]Utilisation : user [add|update|assign-role|list][/]");
            return;
        }

        switch (args[0]) {
            case "add":
                AddUser();
                break;
            case "assign-role":
                if (args.Length < 2 || !int.TryParse(args[1], out int assignId)) {
                    PrintError("Usage: user assign-role <userId>");
                    return;
                }
                AssignRole(assignId);
                break;
            case "update":
                if (args.Length < 2 || !int.TryParse(args[1], out int updateId)) {
                    PrintError("Usage: user update <userId>");
                    return;
                }
                UpdateUser(updateId);
                break;
            case "list":
                ListUsers();
                break;
            default:
                PrintError("Sous-commande inconnue.");
                break;
        }
    }

    private void AddUser() {
        var prenom = Ask("Prénom");
        var nom = Ask("Nom");
        var adresse = Ask("Adresse");
        var tel = Ask("Téléphone");
        var email = Ask("Email");
        var mdp = AskSecret("Mot de passe");
        var metro = AnsiConsole.Ask<int>("ID station métro proche :");

        int userId;
        using (var cmd = new MySqlCommand("INSERT INTO users (nom, prenom, adresse, telephone, email, mdp, metroproche) VALUES (@n, @p, @a, @t, @e, @m, @mp); SELECT LAST_INSERT_ID();", _sqlService.GetConnection())) {
            cmd.Parameters.AddWithValue("@n", nom);
            cmd.Parameters.AddWithValue("@p", prenom);
            cmd.Parameters.AddWithValue("@a", adresse);
            cmd.Parameters.AddWithValue("@t", tel);
            cmd.Parameters.AddWithValue("@e", email);
            cmd.Parameters.AddWithValue("@m", mdp);
            cmd.Parameters.AddWithValue("@mp", metro);
            userId = Convert.ToInt32(cmd.ExecuteScalar());
        }

        string role = SelectRole();
        InsertUserRole(userId, role);
        AnsiConsole.MarkupLine($"[green]✅ Utilisateur {prenom} {nom} ajouté avec le rôle '{role}'.[/]");

        if (role.ToLower() == "client")
            AddClient(userId);
    }

    private void UpdateUser(int id) {
        var prenom = Ask("Nouveau prénom");
        var nom = Ask("Nouveau nom");
        var adresse = Ask("Nouvelle adresse");
        var tel = Ask("Téléphone");
        var mdp = AskSecret("Nouveau mot de passe");

        using var cmd = new MySqlCommand(@"
            UPDATE users SET nom=@n, prenom=@p, adresse=@a, telephone=@t, mdp=@m WHERE user_id=@id", _sqlService.GetConnection());
        cmd.Parameters.AddWithValue("@n", nom);
        cmd.Parameters.AddWithValue("@p", prenom);
        cmd.Parameters.AddWithValue("@a", adresse);
        cmd.Parameters.AddWithValue("@t", tel);
        cmd.Parameters.AddWithValue("@m", mdp);
        cmd.Parameters.AddWithValue("@id", id);

        int rows = cmd.ExecuteNonQuery();
        AnsiConsole.MarkupLine(rows > 0 ? "[green]✅ Utilisateur mis à jour.[/]" : "[yellow]Aucune mise à jour effectuée.[/]");
    }

    private void AssignRole(int userId) {
        string role = SelectRole();
        InsertUserRole(userId, role);
        AnsiConsole.MarkupLine($"[green]✅ Rôle '{role}' assigné à l’utilisateur {userId}.[/]");
        if (role.ToLower() == "client")
            AddClient(userId);
    }

    private void ListUsers() {
        string sort = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Trier par :")
                .AddChoices("Nom (A-Z)", "Adresse", "Achats cumulés"));

        string orderSql = sort switch {
            "Adresse" => "ORDER BY adresse ASC",
            "Achats cumulés" => @"LEFT JOIN commandes c ON u.user_id = c.client_id 
                                   LEFT JOIN plats p ON c.plat_id = p.plat_id 
                                   GROUP BY u.user_id 
                                   ORDER BY IFNULL(SUM(p.prix_par_personne * c.quantite), 0) DESC",
            _ => "ORDER BY nom ASC"
        };

        string query = $@"
            SELECT u.user_id, u.nom, u.prenom, u.adresse, u.email, 
                   IFNULL(SUM(p.prix_par_personne * c.quantite), 0) AS total
            FROM users u
            LEFT JOIN commandes c ON u.user_id = c.client_id
            LEFT JOIN plats p ON c.plat_id = p.plat_id
            {orderSql}";

        var table = new Table().Border(TableBorder.Rounded)
            .AddColumn("ID")
            .AddColumn("Nom")
            .AddColumn("Prénom")
            .AddColumn("Adresse")
            .AddColumn("Email")
            .AddColumn("Montant total");

        using var cmd = new MySqlCommand(query, _sqlService.GetConnection());
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) {
            table.AddRow(
                reader["user_id"].ToString(),
                reader["nom"].ToString(),
                reader["prenom"].ToString(),
                reader["adresse"].ToString(),
                reader["email"].ToString(),
                $"{reader["total"]}€"
            );
        }

        AnsiConsole.Write(table);
    }

    // Helpers
    private string Ask(string label) => AnsiConsole.Ask<string>($"[blue]{label} :[/]");
    private string AskSecret(string label) => AnsiConsole.Prompt(new TextPrompt<string>($"[red]{label} :[/]").Secret());

    private string SelectRole() {
        var roles = new List<string>();
        using var cmd = new MySqlCommand("SELECT role_name FROM roles", _sqlService.GetConnection());
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            roles.Add(reader.GetString(0));

        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Sélectionner un rôle :")
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
                .Title("Type de client :")
                .AddChoices("PARTICULIER", "ENTREPRISE"));

        using var cmd = new MySqlCommand("INSERT INTO clients (client_id, type_client) VALUES (@id, @type)", _sqlService.GetConnection());
        cmd.Parameters.AddWithValue("@id", userId);
        cmd.Parameters.AddWithValue("@type", type);
        cmd.ExecuteNonQuery();
    }

    private void PrintError(string message) =>
        AnsiConsole.MarkupLine($"[red]⛔ {message}[/]");
}
