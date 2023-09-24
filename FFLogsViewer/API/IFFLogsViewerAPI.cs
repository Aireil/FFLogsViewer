namespace FFLogsViewer.API;

/// <summary>
/// Interface to communicate with FFLogsViewer.
/// </summary>
public interface IFFLogsViewerAPI
{
    /// <summary>
    /// Gets api version.
    /// </summary>
    public int APIVersion { get; }

    /// <summary>
    /// Gets or sets a value indicating whether FFLogsViewer API is initialized.
    /// </summary>
    public bool IsInitialized { get; set; }

    /// <summary>
    /// Fetch and open player profile.
    /// </summary>
    /// <param name="name">the name of the player.</param>
    /// <param name="worldId">the id of the world.</param>
    /// <returns>indicator if character was successfully fetched.</returns>
    public bool FetchCharacter(string name, ushort worldId);
}
