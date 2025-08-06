using System.Collections.Generic;

using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;

namespace FFLogsViewer.GUI.Config;

public class OpenWithTab
{
    private static readonly Dictionary<VirtualKey, string> NamedKeys = new()
    {
        { VirtualKey.NO_KEY, "None" },
        { VirtualKey.CONTROL, "Ctrl" },
        { VirtualKey.MENU, "Alt" },
        { VirtualKey.SHIFT, "Shift" },
    };

    public static void Draw()
    {
        if (Service.OpenWithManager.HasLoadingFailed)
        {
            ImGui.Text("Loading failed, this tab is disabled.\n" +
                       "Feel free to report this on the GitHub repo.");

            return;
        }

        ImGui.Text("Automatically fetch the character when opening different windows.");

        var hasChanged = false;
        hasChanged |= ImGui.Checkbox("Open main window if it is closed", ref Service.Configuration.OpenWith.ShouldOpenMainWindow);
        hasChanged |= ImGui.Checkbox("Close main window when one of the enabled windows is closed", ref Service.Configuration.OpenWith.ShouldCloseMainWindow);
        hasChanged |= ImGui.Checkbox("Ignore if the character is yourself", ref Service.Configuration.OpenWith.ShouldIgnoreSelf);

        ImGui.Separator();

        if (ImGui.BeginCombo("Optional key", NamedKeys[Service.Configuration.OpenWith.Key]))
        {
            foreach (var key in NamedKeys)
            {
                if (ImGui.Selectable(key.Value))
                {
                    Service.Configuration.OpenWith.Key = key.Key;
                    hasChanged = true;
                }
            }

            ImGui.EndCombo();
        }

        ImGui.BeginDisabled(Service.Configuration.OpenWith.Key == VirtualKey.NO_KEY);

        if (ImGui.RadioButton("Disable when the key is held", Service.Configuration.OpenWith.IsDisabledWhenKeyHeld))
        {
            Service.Configuration.OpenWith.IsDisabledWhenKeyHeld = true;
            hasChanged = true;
        }

        if (ImGui.RadioButton("Only enable when the key is held", !Service.Configuration.OpenWith.IsDisabledWhenKeyHeld))
        {
            Service.Configuration.OpenWith.IsDisabledWhenKeyHeld = false;
            hasChanged = true;
        }

        ImGui.EndDisabled();

        ImGui.Separator();

        ImGui.Text("Enable for:");
        hasChanged |= ImGui.Checkbox("Adventurer Plate", ref Service.Configuration.OpenWith.IsAdventurerPlateEnabled);
        hasChanged |= ImGui.Checkbox("Examine", ref Service.Configuration.OpenWith.IsExamineEnabled);
        hasChanged |= ImGui.Checkbox("Search Info", ref Service.Configuration.OpenWith.IsSearchInfoEnabled);
        hasChanged |= ImGui.Checkbox("Party Finder", ref Service.Configuration.OpenWith.IsPartyFinderEnabled);

        if (hasChanged)
        {
            Service.OpenWithManager.ReloadState();
            Service.Configuration.Save();
        }
    }
}
