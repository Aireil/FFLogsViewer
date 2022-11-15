using Dalamud.Game.ClientState.Keys;

namespace FFLogsViewer.Model;

public class OpenWith
{
    public bool ShouldOpenMainWindow = true;
    public bool ShouldCloseMainWindow;
    public bool ShouldIgnoreSelf;
    public bool IsAdventurerPlateEnabled;
    public bool IsExamineEnabled;
    public bool IsSearchInfoEnabled;
    public bool IsPartyFinderEnabled;
    public bool IsDisabledWhenKeyHeld = true;
    public VirtualKey Key;

    public bool IsAnyEnabled()
    {
        return this.IsAdventurerPlateEnabled || this.IsExamineEnabled || this.IsSearchInfoEnabled || this.IsPartyFinderEnabled;
    }
}
