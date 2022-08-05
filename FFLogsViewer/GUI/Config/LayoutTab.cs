using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using ImGuiNET;

namespace FFLogsViewer.GUI.Config;

public class LayoutTab
{
    private readonly PopupEntry popupEntry;
    private bool shouldPopupOpen;

    public LayoutTab()
    {
        this.popupEntry = new PopupEntry();
    }

    public void Draw()
    {
        DrawAutoUpdate();
        this.DrawLayoutTable();
        this.DrawFooter();

        // Needed because popups do not open in tables
        if (this.shouldPopupOpen)
        {
            this.shouldPopupOpen = false;
            this.popupEntry.Open();
        }

        this.popupEntry.Draw();
    }

    private static void DrawAutoUpdate()
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
                    Service.MainWindow.ResetSize();
                    Service.MainWindow.ResetSwapGroups();
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
    }

    private void DrawLayoutTable()
    {
        if (ImGui.BeginTable(
                "##ConfigLayoutTable",
                10,
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
            ImGui.TableSetupColumn("Swap ID/#");
            ImGui.TableSetupColumn("##EditCol", ImGuiTableColumnFlags.WidthFixed, 20 * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("##AddCol", ImGuiTableColumnFlags.WidthFixed, 20 * ImGuiHelpers.GlobalScale);
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
                ImGui.Text(layoutEntry.SwapId == string.Empty ? string.Empty : $"{layoutEntry.SwapId}/{layoutEntry.SwapNumber}");

                ImGui.TableNextColumn();
                if (Util.DrawButtonIcon(FontAwesomeIcon.Edit))
                {
                    this.popupEntry.SelectedIndex = i;
                    this.popupEntry.SwitchMode(PopupEntry.Mode.Editing);
                    this.shouldPopupOpen = true;
                }

                ImGui.TableNextColumn();
                if (Util.DrawButtonIcon(FontAwesomeIcon.Plus))
                {
                    this.popupEntry.SelectedIndex = i + 1;
                    this.popupEntry.SwitchMode(PopupEntry.Mode.Adding);
                    this.shouldPopupOpen = true;
                }

                ImGui.PopID();
            }

            ImGui.EndTable();
        }
    }

    private void DrawFooter()
    {
        if (Util.DrawButtonIcon(FontAwesomeIcon.Plus))
        {
            this.popupEntry.SelectedIndex = -1;
            this.popupEntry.SwitchMode(PopupEntry.Mode.Adding);
            this.popupEntry.Open();
        }

        ImGui.SameLine();
        if (Util.DrawButtonIcon(FontAwesomeIcon.Trash))
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
                Service.MainWindow.ResetSize();
                Service.MainWindow.ResetSwapGroups();
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
            if (ImGui.Button("No##DeleteLayout"))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        if (!Service.FfLogsClient.IsTokenValid)
        {
            ImGui.Text("API client is not valid, points information unavailable.");
        }
        else
        {
            if (Service.FfLogsClient.LimitPerHour <= 0)
            {
                Service.FfLogsClient.RefreshRateLimitData();
            }

            var pointsPerRequest = FFLogsClient.EstimateCurrentLayoutPoints();

            ImGui.SameLine();
            ImGui.Text($"Possible requests per hour: {(Service.FfLogsClient.LimitPerHour > 0 ? Service.FfLogsClient.LimitPerHour / pointsPerRequest : "Loading...")}");
            Util.DrawHelp(
                $"Points per hour: {(Service.FfLogsClient.LimitPerHour > 0 ? Service.FfLogsClient.LimitPerHour : "Loading...")}\n" +
                "Points are used by the FF Logs API every time you make a request.\n" +
                "This limit can be increased by subscribing to the FF Logs Patreon.\n" +
                "If you are subscribed, make sure to create the API client on that account.\n" +
                "\n" +
                $"Points used per request: {pointsPerRequest}\n" +
                "Every distinctive zone-difficulty pairs in the layout use some points.");
        }
    }
}
