using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace FFLogsViewer.GUI.Config;

public class ConfigWindow : Window
{
    public LayoutTab LayoutTab = new();

    public ConfigWindow()
        : base("Configuration##FFLogsViewerConfigWindow")
    {
        this.RespectCloseHotkey = true;

        this.Flags = ImGuiWindowFlags.AlwaysAutoResize;
    }

    public override void Draw()
    {
        ImGui.BeginTabBar("ConfigTabs");

        if (ImGui.BeginTabItem("Misc"))
        {
            MiscTab.Draw();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Layout"))
        {
            this.LayoutTab.Draw();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Stats"))
        {
            StatsTab.Draw();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Style"))
        {
            StyleTab.Draw();
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }
}
