using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Colors;
using Dalamud.Utility;
using FFLogsViewer.Model;
using ImGuiNET;

namespace FFLogsViewer.GUI.Main;

public class Table
{
    private Dictionary<string, int> currSwaps = new();

    public void Draw()
    {
        if (!Service.CharDataManager.DisplayedChar.IsDataReady && !Service.MainWindow.IsPartyView)
        {
            return;
        }

        if (Service.MainWindow.IsPartyView)
        {
            this.DrawPartyView();
        }
        else
        {
            this.DrawSingleView();
        }
    }

    public void ResetSwapGroups()
    {
        this.currSwaps = new Dictionary<string, int>();
    }

    private static (string EncounterName, string HoverMessage) GetEncounterInfo(Encounter? encounter, LayoutEntry entry, CharData charData)
    {
        var isValid = charData.Encounters.FirstOrDefault(
            enc => enc.ZoneId == entry.ZoneId)?.IsValid;

        var hoverMessage = string.Empty;
        var encounterName = entry.Alias != string.Empty ? entry.Alias : entry.Encounter;
        if (encounter == null)
        {
            if (isValid != null && !isValid.Value)
            {
                encounterName += " (NS)";
                hoverMessage = "This metric or partition is not supported by this encounter.\nFor some content, aDPS and HPS are the only allowed metrics.";
            }
            else
            {
                encounterName += " (N/A)";
                hoverMessage = "No data available.\n" +
                               "\n" +
                               "This error is expected when the encounter is a recent addition to the layout or not yet listed on FF Logs.\n" +
                               "If neither of these is the case, please " +
                               (Service.Configuration.IsDefaultLayout
                                    ? "report the issue on GitHub."
                                    : "try adding the encounter again.");
            }
        }
        else if (encounter is { IsLockedIn: false })
        {
            encounterName += " (NL)";
            hoverMessage = "Not locked in.";
        }

        return (encounterName, hoverMessage);
    }

    private static void DrawStatHeader(Stat stat, CharData charData)
    {
        ImGui.TableNextColumn();
        if (Service.Configuration.Style.IsHeaderSeparatorDrawn)
        {
            ImGui.Separator();
        }

        if (stat.Type == StatType.BestAmount &&
            stat.Alias.Equals("/metric/", StringComparison.OrdinalIgnoreCase))
        {
            var metricAbbreviation = charData.LoadedMetric != null
                                         ? charData.LoadedMetric.Abbreviation
                                         : Service.Configuration.Metric.Abbreviation;
            Util.CenterText(metricAbbreviation);
        }
        else
        {
            Util.CenterText(stat.Alias != string.Empty ? stat.Alias : stat.Name);
        }

        if (Service.Configuration.Style.IsHeaderSeparatorDrawn)
        {
            ImGui.Separator();
        }
    }

    private static void DrawEncounterStat(Encounter? encounter, Stat stat)
    {
        ImGui.TableNextColumn();

        string? text = null;
        Vector4? color = null;
        switch (stat.Type)
        {
            case StatType.Best:
                text = Util.GetFormattedLog(encounter?.Best, Service.Configuration.NbOfDecimalDigits);
                color = Util.GetLogColor(encounter?.Best);
                break;
            case StatType.Median:
                text = Util.GetFormattedLog(encounter?.Median, Service.Configuration.NbOfDecimalDigits);
                color = Util.GetLogColor(encounter?.Median);
                break;
            case StatType.Kills:
                text = encounter?.Kills?.ToString();
                break;
            case StatType.Fastest:
                if (encounter?.Fastest != null)
                {
                    text = TimeSpan.FromMilliseconds(encounter.Fastest.Value).ToString("mm':'ss");
                }

                break;
            case StatType.BestAmount:
                text = encounter?.BestAmount?.ToString();
                break;
            case StatType.Job:
                text = Service.Configuration.Style.AbbreviateJobNames ? encounter?.Job?.Abbreviation : encounter?.Job?.Name;
                color = encounter?.Job?.Color;
                break;
            case StatType.BestJob:
                text = Service.Configuration.Style.AbbreviateJobNames ? encounter?.BestJob?.Abbreviation : encounter?.BestJob?.Name;
                color = encounter?.BestJob?.Color;
                break;
            case StatType.AllStarsPoints:
                // points have a lot of decimals if fresh log
                text = encounter?.AllStarsPoints?.ToString("0.00");
                break;
            case StatType.AllStarsRank:
                text = encounter?.AllStarsRank?.ToString();
                color = Util.GetLogColor(encounter?.AllStarsRankPercent);
                break;
            case StatType.AllStarsRankPercent:
                text = Util.GetFormattedLog(encounter?.AllStarsRankPercent, Service.Configuration.NbOfDecimalDigits);
                color = Util.GetLogColor(encounter?.AllStarsRankPercent);
                break;
            default:
                text = "?";
                break;
        }

        text ??= encounter is null or { IsValid: false } ? "?" : "-";
        color ??= new Vector4(1, 1, 1, 1);

        Util.CenterTextColored(color.Value, text);
    }

    private static bool IsDefaultSwap(string swapId, int swapNumber)
    {
        return !Service.Configuration.Layout.Exists(entry => entry.SwapId == swapId && entry.SwapNumber < swapNumber);
    }

    private static bool IsFinaleSwap(string swapId, int swapNumber)
    {
        return !Service.Configuration.Layout.Exists(entry => entry.SwapId == swapId && entry.SwapNumber > swapNumber);
    }

    private void DrawPartyView()
    {
        if (ImGui.BeginTable(
                "##MainWindowTablePartyView",
                9,
                Service.Configuration.Style.MainTableFlags))
        {
            ImGui.EndTable();
        }
    }

    private void DrawSingleView()
    {
        var enabledStats = Service.Configuration.Stats.Where(stat => stat.IsEnabled).ToList();
        if (ImGui.BeginTable(
                    "##MainWindowTableSingleView",
                    enabledStats.Count + 1,
                    Service.Configuration.Style.MainTableFlags))
        {
            var displayedEntries = this.GetDisplayedEntries();
            for (var row = 0; row < displayedEntries.Count; row++)
            {
                if (row != 0)
                {
                    ImGui.TableNextRow();
                }

                ImGui.TableNextColumn();

                var entry = displayedEntries[row];
                if (entry.Type == LayoutEntryType.Header)
                {
                    this.DrawStatAlias(entry, row);

                    foreach (var stat in enabledStats)
                    {
                        DrawStatHeader(stat, Service.CharDataManager.DisplayedChar);
                    }
                }
                else if (entry.Type == LayoutEntryType.Encounter)
                {
                    var encounter = Service.CharDataManager.DisplayedChar.Encounters.FirstOrDefault(
                        enc => enc.Id == entry.EncounterId && enc.Difficulty == entry.DifficultyId);
                    var (encounterName, hoverMessage) = GetEncounterInfo(encounter, entry, Service.CharDataManager.DisplayedChar);

                    this.DrawEncounterName(entry, encounterName, hoverMessage, row);

                    foreach (var stat in enabledStats)
                    {
                        DrawEncounterStat(encounter, stat);
                    }
                }
            }

            ImGui.EndTable();
        }
    }

    private void DrawEncounterName(LayoutEntry entry, string encounterName, string hoverMessage, int row)
    {
        if (!hoverMessage.IsNullOrEmpty())
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey);
        }

        this.DrawSwapAlias(entry, encounterName, row);

        if (!hoverMessage.IsNullOrEmpty())
        {
            ImGui.PopStyleColor();
            Util.SetHoverTooltip(hoverMessage);
        }
    }

    private void DrawSwapAlias(LayoutEntry entry, string displayedName, int row)
    {
        if (entry.SwapId == string.Empty)
        {
            ImGui.TextUnformatted(displayedName);
        }
        else
        {
            if (ImGui.Selectable($"{displayedName}##{row}"))
            {
                this.Swap(entry.SwapId, entry.SwapNumber);
            }
        }
    }

    private void DrawStatAlias(LayoutEntry entry, int row)
    {
        if (Service.Configuration.Style.IsHeaderSeparatorDrawn)
        {
            ImGui.Separator();
        }

        this.DrawSwapAlias(entry, entry.Alias, row);

        if (Service.Configuration.Style.IsHeaderSeparatorDrawn)
        {
            ImGui.Separator();
        }
    }

    private List<LayoutEntry> GetDisplayedEntries()
    {
        return Service.Configuration.Layout.Where(entry => entry.SwapId == string.Empty
                                                           || (this.currSwaps.ContainsKey(entry.SwapId) && this.currSwaps[entry.SwapId] == entry.SwapNumber)
                                                           || (!this.currSwaps.ContainsKey(entry.SwapId) && this.AddSwapIfDefault(entry.SwapId, entry.SwapNumber))).ToList();
    }

    private bool AddSwapIfDefault(string swapId, int swapNumber)
    {
        if (IsDefaultSwap(swapId, swapNumber))
        {
            this.currSwaps[swapId] = swapNumber;
            return true;
        }

        return false;
    }

    private void Swap(string swapId, int swapNumber)
    {
        int newSwapNumber;
        if (IsFinaleSwap(swapId, swapNumber))
        {
            newSwapNumber = Service.Configuration.Layout.First(entry => entry.SwapId == swapId && IsDefaultSwap(swapId, entry.SwapNumber)).SwapNumber;
        }
        else
        {
            newSwapNumber = Service.Configuration.Layout
                                   .Where(entry => entry.SwapId == swapId && entry.SwapNumber > swapNumber)
                                   .Select(entry => entry.SwapNumber)
                                   .Distinct()
                                   .MinBy(groupNumber => Math.Abs(swapNumber - groupNumber));
        }

        this.currSwaps[swapId] = newSwapNumber;
        Service.MainWindow.ResetSize();
    }
}
