using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using ImGuiNET;

namespace FFLogsViewer.GUI.Config;

public class LayoutTab
{
    private readonly PopupEntry popupEntry = new();
    private bool shouldPopupOpen;

    public void Draw()
    {
        DrawAutoUpdate();
        this.DrawLayoutTable();
        DrawSwapGroupsError();
        this.DrawFooter();

        // Needed because popups do not open in tables
        if (this.shouldPopupOpen)
        {
            this.shouldPopupOpen = false;
            this.popupEntry.Open();
        }

        this.popupEntry.Draw();
    }

    private static void DrawSwapGroupsError()
    {
        var swapGroups = Service.Configuration.Layout
                                                    .Where(entry => entry.SwapId != string.Empty)
                                                    .Select(entry => (entry.SwapId, entry.SwapNumber))
                                                    .Distinct()
                                                    .ToArray();

        foreach (var (swapId, swapNumber) in swapGroups)
        {
            if (!swapGroups.Any(swapGroup => swapGroup.SwapId == swapId && swapGroup.SwapNumber != swapNumber))
            {
                ImGui.TextColored(ImGuiColors.DalamudRed, $"Swap ID \"{swapId}\" only has a single Swap #, an ID has to have different # or it will just swap with itself.");
            }
        }
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

            Util.DrawHelp("Because you modified the layout, it will not automatically update if the plugin is updated.\nYou will have to add new encounters yourself once they have been added to FF Logs.");

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

    private static void DrawTableHeader()
    {
        var headerColor = ImGui.ColorConvertFloat4ToU32(ImGui.GetStyle().Colors[(int)ImGuiCol.TableHeaderBg]);
        var headerNames = new[] { string.Empty, "Type", "Alias", "Expansion", "Zone", "Encounter", "Difficulty", "Swap ID/#", string.Empty };

        foreach (var headerName in headerNames)
        {
            ImGui.TableNextColumn();
            Util.CenterText(headerName);
            ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, headerColor);
        }
    }

    private void DrawLayoutTable()
    {
        if (ImGui.BeginTable(
                "##ConfigLayoutTable",
                9,
                ImGuiTableFlags.Borders | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.ScrollY,
                new Vector2(-1, 350)))
        {
            ImGui.TableSetupScrollFreeze(0, 1);

            DrawTableHeader();

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

                Util.SetHoverTooltip("Edit");

                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() - ImGui.GetStyle().ItemSpacing.X + 2);
                if (Util.DrawButtonIcon(FontAwesomeIcon.Plus))
                {
                    this.popupEntry.SelectedIndex = i + 1;
                    this.popupEntry.SwitchMode(PopupEntry.Mode.Adding);
                    this.shouldPopupOpen = true;
                }

                Util.SetHoverTooltip("Add below");

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

        Util.SetHoverTooltip("Add");

        ImGui.SameLine();
        if (Util.DrawButtonIcon(FontAwesomeIcon.Trash))
        {
            ImGui.OpenPopup("##DeleteLayout");
        }

        Util.SetHoverTooltip("Delete all");

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

        if (!Service.FFLogsClient.IsTokenValid)
        {
            ImGui.Text("API client is not valid, points information unavailable.");
        }
        else
        {
            if (Service.FFLogsClient.LimitPerHour <= 0)
            {
                Service.FFLogsClient.RefreshRateLimitData();
            }

            var pointsPerRequest = FFLogsClient.EstimateCurrentLayoutPoints();

            ImGui.SameLine();
            ImGui.Text($"Possible requests per hour: {(Service.FFLogsClient.LimitPerHour > 0 ? Service.FFLogsClient.LimitPerHour / pointsPerRequest : "Loading...")}");
            Util.DrawHelp(
                $"Points per hour: {(Service.FFLogsClient.LimitPerHour > 0 ? Service.FFLogsClient.LimitPerHour : "Loading...")}\n" +
                "Points are used by the FF Logs API every time you make a request.\n" +
                "This limit can be increased by subscribing to the FF Logs Patreon.\n" +
                "If you are subscribed, make sure to create the API client on that account.\n" +
                "\n" +
                $"Points used per request: {pointsPerRequest}\n" +
                "Every distinct zone-difficulty pair in the layout uses some points.");
        }
    }
}
