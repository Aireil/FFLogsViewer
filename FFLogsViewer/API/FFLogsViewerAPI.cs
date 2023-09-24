using System;

namespace FFLogsViewer.API;

/// <inheritdoc cref="IFFLogsViewerAPI" />
public class FFLogsViewerAPI : IFFLogsViewerAPI
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FFLogsViewerAPI"/> class.
    /// </summary>
    public FFLogsViewerAPI()
    {
        this.IsInitialized = true;
    }

    /// <inheritdoc />
    public int APIVersion => 1;

    /// <inheritdoc />
    public bool IsInitialized { get; set; }

    /// <inheritdoc />
    public bool FetchCharacter(string name, ushort worldId)
    {
        if (!this.CheckInitialized()) return false;
        try
        {
            Service.MainWindow.Open();
            Service.CharDataManager.DisplayedChar.FetchCharacter(name, worldId);
            return true;
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error(ex, "Failed to fetch character.");
            return false;
        }
    }

    private bool CheckInitialized()
    {
        if (!this.IsInitialized)
        {
            Service.PluginLog.Information("FFLogsViewer API is not initialized.");
            return false;
        }

        return true;
    }
}
