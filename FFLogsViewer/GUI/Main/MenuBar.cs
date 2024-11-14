using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFLogsViewer.Manager;
using ImGuiNET;

namespace FFLogsViewer.GUI.Main;

public class MenuBar
{
    public static void Draw()
    {
        using var style = ImRaii.PushStyle(
            ImGuiStyleVar.ItemSpacing,
            new Vector2(6 * ImGuiHelpers.GlobalScale, ImGui.GetStyle().ItemSpacing.Y));

        if (ImGui.BeginMenuBar())
        {
            string clearHoverTooltip;
            using var font = ImRaii.PushFont(UiBuilder.IconFont);
            if (Service.Configuration.IsCachingEnabled && Service.KeyState[VirtualKey.CONTROL])
            {
                if (ImGui.MenuItem(FontAwesomeIcon.Trash.ToIconString()))
                {
                    Service.FFLogsClient.ClearCache();
                    Service.CharDataManager.FetchLogs();
                    Service.MainWindow.ResetSize();
                }

                clearHoverTooltip = "Clear cache and refresh current logs";
            }
            else
            {
                if (ImGui.MenuItem(FontAwesomeIcon.Eraser.ToIconString()))
                {
                    Service.CharDataManager.Reset();
                    Service.MainWindow.ResetSize();
                }

                clearHoverTooltip = "Clear current view";
                if (Service.Configuration.IsCachingEnabled)
                {
                    clearHoverTooltip += " (hold ctrl to clear cache)";
                }
            }

            font.Pop();
            Util.SetHoverTooltip(clearHoverTooltip);

            font.Push(UiBuilder.IconFont);
            if (ImGui.MenuItem(FontAwesomeIcon.Cog.ToIconString()))
            {
                Service.ConfigWindow.Toggle();
            }

            font.Pop();
            Util.SetHoverTooltip("Configuration");

            var swapViewIcon = Service.MainWindow.IsPartyView ? FontAwesomeIcon.User : FontAwesomeIcon.Users;
            font.Push(UiBuilder.IconFont);
            if (ImGui.MenuItem(swapViewIcon.ToIconString()))
            {
                if (Service.FFLogsClient.IsTokenValid)
                {
                    Service.MainWindow.IsPartyView = !Service.MainWindow.IsPartyView;
                    if (Service.MainWindow.IsPartyView)
                    {
                        Service.CharDataManager.UpdatePartyMembers();
                    }
                    else if (Service.CharDataManager.DisplayedChar.IsInfoSet())
                    {
                        Service.CharDataManager.DisplayedChar.FetchLogs();
                    }

                    Service.MainWindow.ResetSize();
                }
            }

            font.Pop();
            Util.SetHoverTooltip(Service.MainWindow.IsPartyView ? "Swap to single view" : "Swap to party view");

            font.Push(UiBuilder.IconFont);
            if (ImGui.MenuItem(FontAwesomeIcon.History.ToIconString()))
            {
                ImGui.OpenPopup("##History");
            }

            font.Pop();
            Util.SetHoverTooltip("History");

            DrawHistoryPopup();

            var hasTmpSettingChanged = false;

            var jobColor = Service.MainWindow.Job.Color;
            if (!Service.MainWindow.IsPartyView && Service.MainWindow.Job.Name == "Current job")
            {
                jobColor = GameDataManager.Jobs.FirstOrDefault(job => job.Id == Service.CharDataManager.DisplayedChar.LoadedJobId)?.Color ?? jobColor;
            }

            using var color = ImRaii.PushColor(ImGuiCol.Text, jobColor);
            if (ImGui.BeginMenu(Service.MainWindow.Job.Abbreviation))
            {
                foreach (var job in GameDataManager.Jobs)
                {
                    color.Push(ImGuiCol.Text, job.Color);
                    if (ImGui.MenuItem(job.Name))
                    {
                        Service.MainWindow.Job = job;
                        hasTmpSettingChanged = true;
                    }

                    color.Pop();
                }

                ImGui.EndMenu();
            }

            color.Pop();

            if (ImGui.BeginMenu(Service.MainWindow.GetCurrentMetric().Abbreviation))
            {
                foreach (var metric in GameDataManager.AvailableMetrics)
                {
                    if (ImGui.MenuItem(metric.Name))
                    {
                        Service.MainWindow.OverriddenMetric = metric;
                        hasTmpSettingChanged = true;
                    }
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu(Service.MainWindow.Partition.Abbreviation))
            {
                foreach (var partition in GameDataManager.AvailablePartitions)
                {
                    if (ImGui.MenuItem(partition.Name))
                    {
                        Service.MainWindow.Partition = partition;
                        hasTmpSettingChanged = true;
                    }
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu(Service.MainWindow.IsTimeframeHistorical() ? "H%" : "T%"))
            {
                if (ImGui.MenuItem("Historical %"))
                {
                    Service.MainWindow.IsOverridingTimeframe = !Service.Configuration.IsHistoricalDefault;
                    hasTmpSettingChanged = true;
                }

                if (ImGui.MenuItem("Today %"))
                {
                    Service.MainWindow.IsOverridingTimeframe = Service.Configuration.IsHistoricalDefault;
                    hasTmpSettingChanged = true;
                }

                ImGui.EndMenu();
            }

            if (hasTmpSettingChanged)
            {
                Service.MainWindow.ResetSize();
                Service.CharDataManager.FetchLogs();
            }

            var isButtonHidden = Service.Configuration.IsUpdateDismissed2100 || (!ImGui.IsPopupOpen("##UpdateMessage") && DateTime.Now.Second % 2 == 0);
            if (isButtonHidden)
            {
                color.Push(ImGuiCol.Text, Vector4.Zero);
            }

            font.Push(UiBuilder.IconFont);

            ImGui.SameLine();
            if (ImGui.MenuItem(FontAwesomeIcon.InfoCircle.ToIconString()))
            {
                ImGui.OpenPopup("##UpdateMessage");
            }

            font.Pop();

            if (isButtonHidden)
            {
                color.Pop();
            }

            Util.SetHoverTooltip("Update message");

            if (ImGui.BeginPopup("##UpdateMessage", ImGuiWindowFlags.NoMove))
            {
                ImGui.Text("New feature:");
                ImGui.Text("- Party view:");

                font.Push(UiBuilder.IconFont);
                ImGui.SameLine();
                ImGui.Text(FontAwesomeIcon.Users.ToIconString());
                font.Pop();

                ImGui.Text("   Using the 3rd button on this line, you will switch the main window to party view.\n" +
                           "   This view allows you to easily see the logs of your current party.\n" +
                           "   Two layouts are available:\n" +
                           "      - Encounter layout: one stat => all encounters\n" +
                           "      - Stat layout: one encounter => all stats\n");
                ImGui.Text("Misc changes:");
                ImGui.Text("- Cache:");

                font.Push(UiBuilder.IconFont);
                ImGui.SameLine();
                ImGui.Text(FontAwesomeIcon.Trash.ToIconString());
                font.Pop();

                ImGui.Text("   Requests are now cached.\n" +
                           "   The cache is cleared every hour and the 4th button on this line allows you to clear it manually.\n" +
                           "   You can disable this in the settings if you wish to.");
                ImGui.Text("- New style setting to abbreviate job names");
                ImGui.Text("\nIf you encounter any problem or if you have a suggestion, feel free to open an issue on the GitHub:");

                if (ImGui.Button("Open the GitHub repo"))
                {
                    Util.OpenLink("https://github.com/Aireil/FFLogsViewer");
                }

                if (ImGui.Button("Hide##UpdateMessage"))
                {
                    Service.Configuration.IsUpdateDismissed2100 = true;
                    Service.Configuration.Save();
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();
                ImGui.TextColored(ImGuiColors.DalamudGrey, "Click on the same spot to open this again.");

                ImGui.EndPopup();
            }

            ImGui.EndMenuBar();
        }

        style.Pop();
    }

    public static void DrawHistoryPopup()
    {
        if (!ImGui.BeginPopup("##History", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize))
        {
            return;
        }

        var history = Service.HistoryManager.History;
        if (history.Count != 0)
        {
            var tableHeight = 12 * (25 * ImGuiHelpers.GlobalScale);
            if (history.Count < 12)
            {
                tableHeight = -1;
            }

            if (ImGui.BeginTable("##HistoryTable", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(-1, tableHeight)))
            {
                for (var i = 0; i < history.Count; i++)
                {
                    if (i != 0)
                    {
                        ImGui.TableNextRow();
                    }

                    ImGui.TableNextColumn();

                    var historyEntry = history[i];
                    if (ImGui.Selectable($"##PartyListSel{i}", false, ImGuiSelectableFlags.SpanAllColumns, new Vector2(0, 25 * ImGuiHelpers.GlobalScale)))
                    {
                        Service.CharDataManager.DisplayedChar.FetchCharacter($"{historyEntry.FirstName} {historyEntry.LastName}@{historyEntry.WorldName}");
                        ImGui.CloseCurrentPopup();
                    }

                    ImGui.SameLine();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text($"{historyEntry.LastSeen.ToShortDateString()} {historyEntry.LastSeen.ToShortTimeString()}");

                    ImGui.TableNextColumn();

                    ImGui.Text($"{historyEntry.FirstName} {historyEntry.LastName}");

                    ImGui.TableNextColumn();

                    ImGui.Text(historyEntry.WorldName);

                    ImGui.SameLine();
                    ImGui.Dummy(new Vector2(ImGui.GetStyle().ScrollbarSize));
                }

                ImGui.EndTable();
            }
        }
        else
        {
            ImGui.Text("No history");
        }

        ImGui.EndPopup();
    }
}
