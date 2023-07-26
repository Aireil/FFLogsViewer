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
    public bool IsPartyView;

    private readonly HeaderBar headerBar = new();
    private readonly Table table = new();

    public MainWindow()
        : base("FFLogsViewer##FFLogsViewerMainWindow")
    {
        this.RespectCloseHotkey = Service.Configuration.Style.IsCloseHotkeyRespected;
        this.Flags = Service.Configuration.Style.MainWindowFlags;

        this.IsPartyView = Service.Configuration.IsDefaultViewParty;

        this.ResetSize();
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
        if (!Service.FFLogsClient.IsTokenValid && this.IsPartyView)
        {
            this.IsPartyView = false;
        }

        MenuBar.Draw();

        if (!this.IsPartyView)
        {
            this.headerBar.Draw();
        }

        this.table.Draw();
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
