using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using ImGuiNET;

namespace FFLogsViewer.GUI.Config;

public class LayoutTab
{
    private readonly PopupEntry popupEntry;
    private bool isEditButtonPressed;

    public LayoutTab()
    {
        this.popupEntry = new PopupEntry();
    }

    public void Draw()
    {
        ImGui.Text("Auto-update layout: ");
        ImGui.SameLine();
        if (Service.Configuration.IsDefaultLayout)
        {
            ImGui.TextColored(ImGuiColors.HealerGreen, "Enabled");
            Util.DrawHelp("The layout will automatically update with new encounters, if the plugin is updated.");
        }
        else
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, "Disabled");

            Util.DrawHelp("The layout will not automatically update if the plugin is updated.\nYou will have to add new encounters yourself.");

            ImGui.SameLine();
            if (ImGui.SmallButton("Reset layout and resume auto-update"))
            {
                ImGui.OpenPopup("##ResetLayout");
            }

            if (ImGui.BeginPopup("##ResetLayout", ImGuiWindowFlags.NoMove))
            {
                ImGui.Text("This will WIPE any changes you have made.\nAre you sure you want to do this?");
                ImGui.Separator();
                if (ImGui.Button("Yes##ResetLayout"))
                {
                    Service.Configuration.SetDefaultLayout();
                    Service.Configuration.IsDefaultLayout = true;
                    Service.Configuration.Save();
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();
                if (ImGui.Button("No##ResetLayout"))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }
        }

        if (ImGui.BeginTable(
                "##ConfigLayoutTable",
                8,
                ImGuiTableFlags.Borders | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.ScrollY,
                new Vector2(-1, 350)))
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableSetupColumn("##PositionCol", ImGuiTableColumnFlags.WidthFixed, 38 * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("Type");
            ImGui.TableSetupColumn("Alias");
            ImGui.TableSetupColumn("Expansion");
            ImGui.TableSetupColumn("Zone");
            ImGui.TableSetupColumn("Encounter");
            ImGui.TableSetupColumn("Difficulty");
            ImGui.TableSetupColumn("##EditCol", ImGuiTableColumnFlags.WidthFixed, 20 * ImGuiHelpers.GlobalScale);
            ImGui.TableHeadersRow();

            for (var i = 0; i < Service.Configuration.Layout.Count; i++)
            {
                ImGui.PushID($"##ConfigLayoutTableEntry{i}");

                var layoutEntry = Service.Configuration.Layout[i];
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                if (Util.DrawButtonIcon(FontAwesomeIcon.ArrowUp, new Vector2(2, ImGui.GetStyle().FramePadding.Y)))
                {
                    Util.IncList(Service.Configuration.Layout, i);
                    Service.Configuration.IsDefaultLayout = false;
                    Service.Configuration.Save();
                }

                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(2, 0));

                ImGui.SameLine();
                if (Util.DrawButtonIcon(FontAwesomeIcon.ArrowDown, new Vector2(2, ImGui.GetStyle().FramePadding.Y)))
                {
                    Util.DecList(Service.Configuration.Layout, i);
                    Service.Configuration.IsDefaultLayout = false;
                    Service.Configuration.Save();
                }

                ImGui.PopStyleVar();

                ImGui.TableNextColumn();
                ImGui.Text(layoutEntry.Type.ToString());

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(layoutEntry.Alias);

                ImGui.TableNextColumn();
                ImGui.Text(layoutEntry.Expansion);

                ImGui.TableNextColumn();
                ImGui.Text(layoutEntry.Zone);

                ImGui.TableNextColumn();
                ImGui.Text(layoutEntry.Encounter);

                ImGui.TableNextColumn();
                ImGui.Text(layoutEntry.Difficulty);

                ImGui.TableNextColumn();
                if (Util.DrawButtonIcon(FontAwesomeIcon.Edit, new Vector2(2, ImGui.GetStyle().FramePadding.Y)))
                {
                    this.popupEntry.EditingIndex = i;
                    this.popupEntry.SwitchMode(PopupEntry.Mode.Editing);
                    this.isEditButtonPressed = true;
                }

                ImGui.PopID();
            }

            ImGui.EndTable();
        }

        if (Util.DrawButtonIcon(FontAwesomeIcon.Plus, new Vector2(2, ImGui.GetStyle().FramePadding.Y)))
        {
            this.popupEntry.SwitchMode(PopupEntry.Mode.Adding);
            this.popupEntry.Open();
        }

        ImGui.SameLine();
        if (Util.DrawButtonIcon(FontAwesomeIcon.Trash, new Vector2(2, ImGui.GetStyle().FramePadding.Y)))
        {
            ImGui.OpenPopup("##DeleteLayout");
        }

        if (ImGui.BeginPopup("##DeleteLayout", ImGuiWindowFlags.NoMove))
        {
            ImGui.Text("This will DELETE all the entries.\nAre you sure you want to do this?");
            ImGui.Separator();
            if (ImGui.Button("Yes##DeleteLayout"))
            {
                Service.Configuration.Layout = new List<LayoutEntry>();
                Service.Configuration.IsDefaultLayout = false;
                Service.Configuration.Save();
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
            if (ImGui.Button("No##DeleteLayout"))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        // Needed because popups do not open in tables
        if (this.isEditButtonPressed)
        {
            this.isEditButtonPressed = false;
            this.popupEntry.Open();
        }

        this.popupEntry.Draw();
    }
}
