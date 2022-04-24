using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Windowing;
using FFLogsViewer.Manager;
using ImGuiNET;

namespace FFLogsViewer.GUI.Main;

public class MainWindow : Window
{
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
        Service.CharDataManager.DisplayedChar.Job = GameDataManager.GetDefaultJob();
        Service.CharDataManager.DisplayedChar.OverriddenMetric = null;
    }

    public override void Draw()
    {
        MenuBar.Draw();

        this.headerBar.Draw();

        if (Service.CharDataManager.DisplayedChar.IsDataReady || Service.Configuration.Style.IsTableAlwaysDrawn)
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
}
