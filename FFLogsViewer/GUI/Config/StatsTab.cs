﻿using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using FFLogsViewer.Manager;
using FFLogsViewer.Model;
using ImGuiNET;

namespace FFLogsViewer.GUI.Config;

public class StatsTab
{
    public static void Draw()
    {
        var hasChanged = false;
        ImGui.SetNextItemWidth(GameDataManager.AvailableMetrics.Select(metric => ImGui.CalcTextSize(metric.Name).X).Max() + (30 * ImGuiHelpers.GlobalScale));
        if (ImGui.BeginCombo("Default Metric", Service.Configuration.Metric.Name))
        {
            foreach (var metric in GameDataManager.AvailableMetrics)
            {
                if (ImGui.Selectable(metric.Name))
                {
                    Service.Configuration.Metric = metric;
                    hasChanged = true;
                }
            }

            ImGui.EndCombo();
        }

        Util.SetHoverTooltip("Can be temporarily overridden in the main window");

        ImGui.SetNextItemWidth(GameDataManager.AvailableMetrics.Select(metric => ImGui.CalcTextSize(metric.Name).X).Max() + (30 * ImGuiHelpers.GlobalScale));
        if (ImGui.BeginCombo("Default Timeframe", Service.Configuration.IsHistoricalDefault ? "Historical %" : "Today %"))
        {
            if (ImGui.Selectable("Historical %"))
            {
                Service.Configuration.IsHistoricalDefault = true;
                hasChanged = true;
            }

            if (ImGui.Selectable("Today %"))
            {
                Service.Configuration.IsHistoricalDefault = false;
                hasChanged = true;
            }

            ImGui.EndCombo();
        }

        Util.SetHoverTooltip("Can be temporarily overridden in the main window");

        if (ImGui.BeginTable(
                "##ConfigStatsTable",
                4,
                ImGuiTableFlags.Borders | ImGuiTableFlags.SizingStretchProp,
                new Vector2(-1, -1)))
        {
            ImGui.TableSetupScrollFreeze(0, 1);

            DrawTableHeader();

            var minAliasSize = Service.Configuration.Stats.Select(stat => ImGui.CalcTextSize(stat.Alias).X).Prepend(ImGui.CalcTextSize("Alias").X).Max() + 10;
            for (var i = 0; i < Service.Configuration.Stats.Count; i++)
            {
                ImGui.PushID($"##ConfigStatsTable{i}");

                var stat = Service.Configuration.Stats[i];
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                if (Util.DrawButtonIcon(FontAwesomeIcon.ArrowUp, new Vector2(2, ImGui.GetStyle().FramePadding.Y)))
                {
                    Util.IncList(Service.Configuration.Stats, i);
                    hasChanged = true;
                }

                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(2, 0));

                ImGui.SameLine();
                if (Util.DrawButtonIcon(FontAwesomeIcon.ArrowDown, new Vector2(2, ImGui.GetStyle().FramePadding.Y)))
                {
                    Util.DecList(Service.Configuration.Stats, i);
                    hasChanged = true;
                }

                ImGui.PopStyleVar();

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(minAliasSize);
                hasChanged |= ImGui.InputText("##Alias", ref stat.Alias, 20);

                if (stat.Type == StatType.BestAmount)
                {
                    Util.SetHoverTooltip("If the alias is /metric/, it will be replaced by the current metric");
                }

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(stat.Name);

                ImGui.TableNextColumn();
                var offset = (ImGui.GetContentRegionAvail().X - (22 * ImGuiHelpers.GlobalScale)) / 2;
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
                if (ImGui.Checkbox("##CheckBox", ref stat.IsEnabled))
                {
                    Service.MainWindow.ResetSize();
                    hasChanged = true;
                }

                ImGui.PopID();
            }

            ImGui.EndTable();
        }

        if (hasChanged)
        {
            Service.Configuration.Save();
        }
    }

    private static void DrawTableHeader()
    {
        var headerColor = ImGui.ColorConvertFloat4ToU32(ImGui.GetStyle().Colors[(int)ImGuiCol.TableHeaderBg]);
        var headerNames = new[] { string.Empty, "Alias", "Stat", "Enabled" };

        foreach (var headerName in headerNames)
        {
            ImGui.TableNextColumn();
            Util.CenterText(headerName);
            ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, headerColor);
        }
    }
}
