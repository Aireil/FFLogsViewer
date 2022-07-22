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

    private readonly HeaderBar headerBar = new();

    public MainWindow()
        : base("FFLogsViewer##FFLogsViewerMainWindow")
    {
        this.RespectCloseHotkey = Service.Configuration.Style.IsCloseHotkeyRespected;

        this.Flags = Service.Configuration.Style.MainWindowFlags;

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

    public override void OnOpen()
    {
        this.ResetTemporarySettings();
    }

    public override void Draw()
    {
        MenuBar.Draw();

        this.headerBar.Draw();

        if (Service.CharDataManager.DisplayedChar.IsDataReady)
        {
            Table.Draw();
        }
    }

    public void SetErrorMessage(string message)
    {
        this.headerBar.ErrorMessage = message;
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
        this.Partition = GameDataManager.GetDefaultPartition();
    }
}
