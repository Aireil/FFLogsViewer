using ImGuiNET;

namespace FFLogsViewer.GUI.Config;

public class OpenWithTab
{
    public static void Draw()
    {
        if (Service.OpenWithManager.HasLoadingFailed)
        {
            ImGui.Text("Loading failed, this tab is disabled.\n" +
                       "Feel free to report this on the GitHub repo.");

            return;
        }

        ImGui.Text("Automatically fetch the character when opening different windows.");
        ImGui.Text("Only works if the main window is opened.");

        var hasChanged = false;
        hasChanged |= ImGui.Checkbox("Open main window if it is closed", ref Service.Configuration.OpenWith.ShouldOpenMainWindow);
        hasChanged |= ImGui.Checkbox("Close main window when one of the enabled windows is closed", ref Service.Configuration.OpenWith.ShouldCloseMainWindow);

        ImGui.Separator();

        ImGui.Text("Enable for:");
        hasChanged |= ImGui.Checkbox("Adventurer Plate", ref Service.Configuration.OpenWith.IsAdventurerPlateEnabled);
        hasChanged |= ImGui.Checkbox("Examine", ref Service.Configuration.OpenWith.IsExamineEnabled);
        hasChanged |= ImGui.Checkbox("Search Info", ref Service.Configuration.OpenWith.IsSearchInfoEnabled);

        if (hasChanged)
        {
            Service.OpenWithManager.ReloadState();
            Service.Configuration.Save();
        }
    }
}
