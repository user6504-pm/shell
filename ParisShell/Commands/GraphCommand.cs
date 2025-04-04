using ParisShell.Graph;
using ParisShell.Services;

using ParisShell;

/// <summary>
/// Command to build and display the current graph using station data.
/// </summary>
internal class GraphCommand : ICommand
{
    /// <summary>
    /// The name used to invoke the graph command.
    /// </summary>
    public string Name => "graph";

    private readonly SqlService _sqlService;

    /// <summary>
    /// Initializes the GraphCommand with the SQL service.
    /// </summary>
    public GraphCommand(SqlService sqlService)
    {
        _sqlService = sqlService;
    }

    /// <summary>
    /// Executes the graph display logic. Requires a valid database connection.
    /// </summary>
    public void Execute(string[] args)
    {
        if (!_sqlService.IsConnected)
        {
            Shell.PrintError("Must be connected to a database.");
        }
        GraphLoader.ConstruireEtAfficherGraph(_sqlService.GetConnection());
    }
}
