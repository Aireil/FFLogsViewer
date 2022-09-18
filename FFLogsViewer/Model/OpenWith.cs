namespace FFLogsViewer.Model;

public class OpenWith
{
    public bool ShouldOpenMainWindow;
    public bool ShouldCloseMainWindow;
    public bool IsAdventurerPlateEnabled;
    public bool IsExamineEnabled;
    public bool IsSearchInfoEnabled;

    public bool IsAnyEnabled()
    {
        return this.IsAdventurerPlateEnabled || this.IsExamineEnabled || this.IsSearchInfoEnabled;
    }
}
