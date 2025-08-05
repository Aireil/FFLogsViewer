using Dalamud.Bindings.ImGui;

namespace FFLogsViewer.GUI.Config;

public class StyleTab
{
    public static void Draw()
    {
        var style = Service.Configuration.Style;
        var hasStyleChanged = false;
        ImGui.Text("Main window:");

        ImGui.Indent();

        if (ImGui.BeginCombo("Default view", Service.Configuration.IsDefaultViewParty ? "Party view" : "Single view"))
        {
            if (ImGui.Selectable("Single view"))
            {
                Service.Configuration.IsDefaultViewParty = false;
                Service.MainWindow.IsPartyView = false;
                hasStyleChanged = true;
            }

            if (ImGui.Selectable("Party view"))
            {
                Service.Configuration.IsDefaultViewParty = true;
                Service.MainWindow.IsPartyView = true;
                hasStyleChanged = true;
            }

            ImGui.EndCombo();
        }

        Util.DrawHelp("Default view when opening the window with /fflogs if the view has not been changed yet since last plugin restart.");

        var hideInCombat = Service.Configuration.HideInCombat;
        if (ImGui.Checkbox("Hide in combat##HideInCombat", ref hideInCombat))
        {
            Service.Configuration.HideInCombat = hideInCombat;
            hasStyleChanged = true;
        }

        if (ImGui.Checkbox("Close window with esc", ref style.IsCloseHotkeyRespected))
        {
            Service.MainWindow.RespectCloseHotkey = style.IsCloseHotkeyRespected;
            hasStyleChanged = true;
        }

        var tmpWindowFlags = (int)style.MainWindowFlags;
        if (ImGui.CheckboxFlags("No titlebar", ref tmpWindowFlags, (int)ImGuiWindowFlags.NoTitleBar))
        {
            style.MainWindowFlags = (ImGuiWindowFlags)tmpWindowFlags;
            Service.MainWindow.Flags = style.MainWindowFlags;
            hasStyleChanged = true;
        }

        if (ImGui.RadioButton("Fixed size", style.IsSizeFixed))
        {
            style.IsSizeFixed = true;
            style.MainWindowFlags &= ~ImGuiWindowFlags.AlwaysAutoResize;
            Service.MainWindow.Flags = style.MainWindowFlags;
            hasStyleChanged = true;
        }

        ImGui.SameLine();
        var isAutoResizeFlagSet = (style.MainWindowFlags & ImGuiWindowFlags.AlwaysAutoResize) != 0;
        if (ImGui.RadioButton("Auto resize", isAutoResizeFlagSet && !style.IsSizeFixed))
        {
            style.IsSizeFixed = false;
            style.MainWindowFlags |= ImGuiWindowFlags.AlwaysAutoResize;
            Service.MainWindow.Flags = style.MainWindowFlags;
            Service.MainWindow.ResetSize();
            hasStyleChanged = true;
        }

        Util.DrawHelp("Will expand and shrink based on content.");

        ImGui.SameLine();
        if (ImGui.RadioButton("Fixed min size", !isAutoResizeFlagSet && !style.IsSizeFixed))
        {
            style.IsSizeFixed = false;
            style.MainWindowFlags &= ~ImGuiWindowFlags.AlwaysAutoResize;
            Service.MainWindow.Flags = style.MainWindowFlags;
            hasStyleChanged = true;
        }

        Util.DrawHelp("Will not be under, will expand and shrink a little bit to accomodate long names.");

        if (style.IsSizeFixed)
        {
            ImGui.Indent();

            if (ImGui.CheckboxFlags("No resize", ref tmpWindowFlags, (int)ImGuiWindowFlags.NoResize))
            {
                style.MainWindowFlags = (ImGuiWindowFlags)tmpWindowFlags;
                Service.MainWindow.Flags = style.MainWindowFlags;
                hasStyleChanged = true;
            }

            ImGui.Unindent();
        }

        if (!isAutoResizeFlagSet && !style.IsSizeFixed)
        {
            ImGui.Indent();
            hasStyleChanged |= ImGui.SliderFloat("Min size", ref Service.Configuration.Style.MinMainWindowWidth, 1, 2000);

            ImGui.Unindent();
        }

        ImGui.Unindent();

        ImGui.Text("Main window table:");

        ImGui.Indent();

        ImGui.AlignTextToFramePadding();
        ImGui.Text("Number of decimal digits for logs: ");
        for (var i = 0; i <= 2; i++)
        {
            ImGui.SameLine();
            if (ImGui.RadioButton(i + "##NbOfDecimalDigits", Service.Configuration.NbOfDecimalDigits == i))
            {
                Service.Configuration.NbOfDecimalDigits = i;
                hasStyleChanged = true;
            }
        }

        hasStyleChanged |= ImGui.Checkbox("Abbreviate job names", ref style.AbbreviateJobNames);
        hasStyleChanged |= ImGui.Checkbox("Header separator", ref style.IsHeaderSeparatorDrawn);

        var tmpTableFlags2 = (int)style.MainTableFlags;
        if (ImGui.CheckboxFlags("Alternate row background##TableFlag", ref tmpTableFlags2, (int)ImGuiTableFlags.RowBg))
        {
            style.MainTableFlags = (ImGuiTableFlags)tmpTableFlags2;
            hasStyleChanged = true;
        }

        if (ImGui.Button("Borders customization"))
        {
            ImGui.OpenPopup("##Borders");
        }

        if (ImGui.BeginPopup("##Borders", ImGuiWindowFlags.NoMove))
        {
            var tmpTableFlags = (int)style.MainTableFlags;
            var hasChanged = false;
            hasChanged |= ImGui.CheckboxFlags("Borders##TableFlag", ref tmpTableFlags, (int)ImGuiTableFlags.Borders);
            hasChanged |= ImGui.CheckboxFlags("BordersH##TableFlag", ref tmpTableFlags, (int)ImGuiTableFlags.BordersH);
            hasChanged |= ImGui.CheckboxFlags("BordersV##TableFlag", ref tmpTableFlags, (int)ImGuiTableFlags.BordersV);
            hasChanged |= ImGui.CheckboxFlags("BordersInner##TableFlag", ref tmpTableFlags, (int)ImGuiTableFlags.BordersInner);
            hasChanged |= ImGui.CheckboxFlags("BordersOuter##TableFlag", ref tmpTableFlags, (int)ImGuiTableFlags.BordersOuter);
            hasChanged |= ImGui.CheckboxFlags("BordersInnerH##TableFlag", ref tmpTableFlags, (int)ImGuiTableFlags.BordersInnerH);
            hasChanged |= ImGui.CheckboxFlags("BordersInnerV##TableFlag", ref tmpTableFlags, (int)ImGuiTableFlags.BordersInnerV);
            hasChanged |= ImGui.CheckboxFlags("BordersOuterH##TableFlag", ref tmpTableFlags, (int)ImGuiTableFlags.BordersOuterH);
            hasChanged |= ImGui.CheckboxFlags("BordersOuterV##TableFlag", ref tmpTableFlags, (int)ImGuiTableFlags.BordersOuterV);
            if (hasChanged)
            {
                style.MainTableFlags = (ImGuiTableFlags)tmpTableFlags;
                hasStyleChanged = true;
            }
        }

        ImGui.Unindent();

        ImGui.Text("Party view:");

        ImGui.Indent();

        if (ImGui.Checkbox("Include yourself in the party view", ref style.IsLocalPlayerInPartyView))
        {
            Service.CharDataManager.UpdatePartyMembers();
            hasStyleChanged = true;
        }

        ImGui.Unindent();
        if (hasStyleChanged)
        {
            Service.Configuration.Save();
        }
    }
}
