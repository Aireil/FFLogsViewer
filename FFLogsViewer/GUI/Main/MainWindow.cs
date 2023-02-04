using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Windowing;
using FFLogsViewer.Manager;
using FFLogsViewer.Model;
using ImGuiNET;

namespace FFLogsViewer.GUI.Main;

public class MainWindow : Window
{
    public Job Job = GameDataManager.GetDefaultJob();
    public Partition Partition = GameDataManager.GetDefaultPartition();
    public Metric? OverriddenMetric;
    public bool IsOverridingTimeframe;

    private readonly HeaderBar headerBar = new();
    private readonly Table table = new();

    public MainWindow()
        : base("FFLogsViewer##FFLogsViewerMainWindow")
    {
        this.RespectCloseHotkey = Service.Configuration.Style.IsCloseHotkeyRespected;

        this.Flags = Service.Configuration.Style.MainWindowFlags;

        this.ResetSize();
    }

    public static void ResetError()
    {
        Service.CharDataManager.DisplayedChar.CharError = null;
    }

    public static string GetErrorMessage()
    {
        return GetErrorMessage(Service.CharDataManager.DisplayedChar);
    }

    public static string GetErrorMessage(CharData charData)
    {
        return charData.CharError switch
        {
            CharacterError.CharacterNotFoundFFLogs => "Character not found on FF Logs",
            CharacterError.CharacterNotFound => "Character not found",
            CharacterError.ClipboardError => "Couldn't get clipboard text",
            CharacterError.GenericError => "An error occured, please try again",
            CharacterError.HiddenLogs => $"{charData.FirstName} {charData.LastName}@{charData.WorldName}'s logs are hidden",
            CharacterError.InvalidTarget => "Not a valid target",
            CharacterError.InvalidWorld => "World not supported or invalid",
            CharacterError.MalformedQuery => "Malformed GraphQL query.",
            CharacterError.MissingInputs => "Please fill first name, last name, and world",
            CharacterError.NetworkError => "Networking error, please try again",
            CharacterError.Unauthenticated => "API Client not valid, check config",
            CharacterError.Unreachable => "Could not reach FF Logs servers",
            CharacterError.WorldNotFound => "World not found",
            _ => "If you see this, something went wrong",
        };
    }

    public override bool DrawConditions()
    {
        if (Service.Configuration.HideInCombat && Service.Condition[ConditionFlag.InCombat])
        {
            return false;
        }

        return true;
    }

    public void Open(bool takeFocus = true)
    {
        if (!takeFocus)
        {
            this.Flags |= ImGuiWindowFlags.NoFocusOnAppearing;
        }

        this.IsOpen = true;
    }

    public override void OnOpen()
    {
        this.ResetSize();
        this.ResetTemporarySettings();
    }

    public override void OnClose()
    {
        this.Flags &= ~ImGuiWindowFlags.NoFocusOnAppearing;
    }

    public override void Draw()
    {
        MenuBar.Draw();

        this.headerBar.Draw();

        if (Service.CharDataManager.DisplayedChar.IsDataReady)
        {
            this.table.Draw();
        }
    }

    public void ResetSize()
    {
        if (!Service.Configuration.Style.IsSizeFixed &&
            (Service.Configuration.Style.MainWindowFlags & ImGuiWindowFlags.AlwaysAutoResize) != 0)
        {
            this.headerBar.ResetSizeCount = 5;
        }
    }

    public void ResetTemporarySettings()
    {
        this.Job = GameDataManager.GetDefaultJob();
        this.OverriddenMetric = null;
        this.IsOverridingTimeframe = false;
        this.Partition = GameDataManager.GetDefaultPartition();
    }

    public Metric GetCurrentMetric()
    {
        return this.OverriddenMetric ?? Service.Configuration.Metric;
    }

    public bool IsTimeframeHistorical()
    {
        return this.IsOverridingTimeframe ? !Service.Configuration.IsHistoricalDefault : Service.Configuration.IsHistoricalDefault;
    }

    public void ResetSwapGroups()
    {
        this.table.ResetSwapGroups();
    }
}
