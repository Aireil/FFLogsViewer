using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Utility;
using FFLogsViewer.Model;
using ImGuiNET;
using FFLogsViewer_Util = FFLogsViewer.Util;

namespace FFLogsViewer.GUI.Main;

public class Table
{
    private Dictionary<string, int> currSwaps = new();
    private Stat currentStat = Service.Configuration.Stats.First(stat => stat.Type == StatType.Best);
    private LayoutEntry currentEncounter = Service.Configuration.Layout.First(entry => entry.Type == LayoutEntryType.Encounter);

    public void Draw()
    {
        if (Service.MainWindow.IsPartyView)
        {
            this.DrawPartyView();
        }
        else if (Service.CharDataManager.DisplayedChar.IsDataReady)
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

    private static void DrawStatHeader(Stat stat, CharData? charData, bool drawSeparator = true)
    {
        if (drawSeparator && Service.Configuration.Style.IsHeaderSeparatorDrawn)
        {
            ImGui.Separator();
        }

        var metricAbbreviation = Util.GetMetricAbbreviation(charData);
        Util.CenterText(stat.GetFinalAlias(metricAbbreviation));

        if (drawSeparator && Service.Configuration.Style.IsHeaderSeparatorDrawn)
        {
            ImGui.Separator();
        }
    }

    private static void DrawEncounterStat(Encounter? encounter, Stat stat, string hoverMessage)
    {
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

        Util.CenterText(text, color.Value);
        if (hoverMessage != string.Empty)
        {
            Util.SetHoverTooltip(hoverMessage);
        }
    }

    private static void DrawHeaderSeparator()
    {
        if (Service.Configuration.Style.IsHeaderSeparatorDrawn)
        {
            ImGui.Separator();
        }
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
        if (Service.Configuration.Layout.Count == 0)
        {
            if (Util.CenterSelectable("You have no layout set up. Click to open settings."))
            {
                Service.ConfigWindow.IsOpen = true;
            }

            return;
        }

        if (ImGui.Button("Update"))
        {
            Service.CharDataManager.UpdatePartyMembers();
        }

        ImGui.SameLine();
        if (ImGui.Button("Change layout"))
        {
            Service.Configuration.IsStatLayout = !Service.Configuration.IsStatLayout;
            Service.Configuration.Save();
        }

        if (Service.Configuration.IsStatLayout)
        {
            this.DrawStatLayout();
        }
        else
        {
            this.DrawEncounterLayout();
        }
    }

    private void DrawStatLayout()
    {
        var currentParty = Service.CharDataManager.PartyMembers;
        var displayedEntries = this.GetDisplayedEntries();

        if (ImGui.BeginTable(
                "##MainWindowTablePartyViewStatLayout",
                8,
                Service.Configuration.Style.MainTableFlags))
        {
            ImGui.TableNextColumn();

            ImGui.SetNextItemWidth(Service.Configuration.Stats.Select(metric => ImGui.CalcTextSize(metric.Alias).X).Max() + (30 * ImGuiHelpers.GlobalScale));
            var metricAbbreviation = Util.GetMetricAbbreviation(currentParty.FirstOrDefault());

            if (ImGui.BeginCombo(string.Empty, this.currentStat.GetFinalAlias(metricAbbreviation)))
            {
                foreach (var stat in Service.Configuration.Stats.Where(stat => stat.IsEnabled))
                {
                    if (ImGui.Selectable(stat.Name))
                    {
                        this.currentStat = stat;
                    }
                }

                ImGui.EndCombo();
            }

            if (Util.DrawButtonIcon(FontAwesomeIcon.ArrowLeft, new Vector2(3 * ImGuiHelpers.GlobalScale)))
            {
                this.ShiftCurrentStat(-1);
            }

            ImGui.SameLine();
            if (Util.DrawButtonIcon(FontAwesomeIcon.ArrowRight, new Vector2(3 * ImGuiHelpers.GlobalScale)))
            {
                this.ShiftCurrentStat(1);
            }

            var separatorY = ImGui.GetCursorPosY();
            if (Service.Configuration.Style.IsHeaderSeparatorDrawn && displayedEntries[0].Type != LayoutEntryType.Header)
            {
                ImGui.Separator();
            }

            for (var i = 0; i < 7; i++)
            {
                var charData = i < currentParty.Count ? currentParty[i] : null;

                ImGui.TableNextColumn();

                if (charData != null)
                {
                    if (Util.CenterSelectableWithError(charData.Abbreviation + $"##Selectable{i}", charData))
                    {
                        Util.OpenLink(charData);
                    }

                    if (charData.CharError == null)
                    {
                        Util.SetHoverTooltip($"{charData.FirstName} {charData.LastName}@{charData.WorldName}");
                    }
                }
                else
                {
                    Util.CenterText("-");
                }

                var iconSize = 25 * ImGuiHelpers.GlobalScale;
                Util.CenterCursor(iconSize);
                var icon = Service.GameDataManager.JobIconsManager.GetJobIcon(charData?.JobId ?? 0);
                if (icon != null)
                {
                    ImGui.Image(icon.ImGuiHandle, new Vector2(iconSize));
                }
                else
                {
                    ImGui.Text("(?)");
                }

                ImGui.SetCursorPosY(separatorY);
                if (Service.Configuration.Style.IsHeaderSeparatorDrawn && displayedEntries[0].Type != LayoutEntryType.Header)
                {
                    ImGui.Separator();
                }
            }

            for (var row = 0; row < displayedEntries.Count; row++)
            {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();

                var entry = displayedEntries[row];
                if (entry.Type == LayoutEntryType.Header)
                {
                    this.DrawStatAlias(entry, row);

                    for (var i = 0; i < 7; i++)
                    {
                        ImGui.TableNextColumn();
                        var charData = i < currentParty.Count ? currentParty[i] : null;
                        DrawStatHeader(this.currentStat, charData);
                    }
                }
                else if (entry.Type == LayoutEntryType.Encounter)
                {
                    this.DrawEncounterName(entry, entry.Alias == string.Empty ? entry.Encounter : entry.Alias, string.Empty, row);

                    for (var i = 0; i < 7; i++)
                    {
                        ImGui.TableNextColumn();
                        var charData = i < currentParty.Count ? currentParty[i] : null;
                        if (charData is { IsDataLoading: true })
                        {
                            Util.CenterText("...");
                            continue;
                        }

                        if (charData is not { IsDataReady: true })
                        {
                            Util.CenterTextWithError("-", charData);
                            continue;
                        }

                        var encounter = charData.Encounters.FirstOrDefault(
                            enc => enc.Id == entry.EncounterId && enc.Difficulty == entry.DifficultyId);

                        var (_, hoverMessage) = GetEncounterInfo(encounter, entry, charData);
                        DrawEncounterStat(encounter, this.currentStat, hoverMessage);
                    }
                }
            }

            ImGui.EndTable();
        }
    }

    private void DrawEncounterLayout()
    {
        var currentParty = Service.CharDataManager.PartyMembers;
        var enabledStats = Service.Configuration.Stats.Where(stat => stat.IsEnabled).ToList();

        if (ImGui.BeginTable(
                "##MainWindowTablePartyViewEncounterLayout",
                enabledStats.Count + 1,
                Service.Configuration.Style.MainTableFlags))
        {
            ImGui.TableNextColumn();

            ImGui.SetNextItemWidth(Service.Configuration.Layout.Select(entry => ImGui.CalcTextSize(entry.Alias != string.Empty ? entry.Alias : entry.Encounter).X).Max() + (30 * ImGuiHelpers.GlobalScale));
            var encounterAbbreviation = this.currentEncounter.Alias != string.Empty
                                            ? this.currentEncounter.Alias
                                            : this.currentEncounter.Encounter;

            if (ImGui.BeginCombo(string.Empty, encounterAbbreviation))
            {
                for (var i = 0; i < Service.Configuration.Layout.Count; i++)
                {
                    var entry = Service.Configuration.Layout[i];
                    if (entry.Type == LayoutEntryType.Header)
                    {
                        ImGui.BeginDisabled();
                        ImGui.Selectable($"- {entry.Alias}##{i}");
                        ImGui.EndDisabled();
                    }
                    else
                    {
                        if (ImGui.Selectable($"{entry.Encounter}##{i}"))
                        {
                            this.currentEncounter = entry;
                        }
                    }
                }

                ImGui.EndCombo();
            }

            if (Util.DrawButtonIcon(FontAwesomeIcon.ArrowLeft, new Vector2(3 * ImGuiHelpers.GlobalScale)))
            {
                this.ShiftCurrentEncounter(-1);
            }

            ImGui.SameLine();
            if (Util.DrawButtonIcon(FontAwesomeIcon.ArrowRight, new Vector2(3 * ImGuiHelpers.GlobalScale)))
            {
                this.ShiftCurrentEncounter(1);
            }

            var separatorY = ImGui.GetCursorPosY();
            if (Service.Configuration.Style.IsHeaderSeparatorDrawn)
            {
                ImGui.Separator();
            }

            foreach (var stat in enabledStats)
            {
                ImGui.TableNextColumn();
                var offsetY = 2 * ImGui.GetFontSize() / 3.0f;
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + offsetY);
                DrawStatHeader(stat, currentParty.Count > 0 ? currentParty[0] : null, false);

                if (Service.Configuration.Style.IsHeaderSeparatorDrawn)
                {
                    ImGui.SetCursorPosY(separatorY);
                    ImGui.Separator();
                }
            }

            for (var i = 0; i < 7; i++)
            {
                var charData = i < currentParty.Count ? currentParty[i] : null;

                if (i != 0)
                {
                    ImGui.TableNextRow();
                }

                ImGui.TableNextColumn();
                var iconSize = 25 * ImGuiHelpers.GlobalScale;
                var middleCursorPosY = ImGui.GetCursorPosY() + (iconSize / 2) - (ImGui.CalcTextSize("R").Y / 2);
                var icon = Service.GameDataManager.JobIconsManager.GetJobIcon(charData?.JobId ?? 0);
                if (icon != null)
                {
                    ImGui.Image(icon.ImGuiHandle, new Vector2(iconSize));
                }
                else
                {
                    ImGui.SetCursorPosY(middleCursorPosY);
                    ImGui.Text("(?)");
                }

                ImGui.SameLine();
                ImGui.SetCursorPosY(middleCursorPosY);
                if (charData != null)
                {
                    if (Util.SelectableWithError($"{charData.FirstName} {charData.LastName}##Selectable{i}", charData))
                    {
                        Util.OpenLink(charData);
                    }

                    if (charData.CharError == null)
                    {
                        Util.SetHoverTooltip($"{charData.FirstName} {charData.LastName}@{charData.WorldName}");
                    }
                }
                else
                {
                    ImGui.Text("-");
                }

                foreach (var stat in enabledStats)
                {
                    ImGui.TableNextColumn();
                    ImGui.SetCursorPosY(middleCursorPosY);

                    if (charData is { IsDataLoading: true })
                    {
                        Util.CenterText("...");
                        continue;
                    }

                    if (charData is not { IsDataReady: true })
                    {
                        Util.CenterTextWithError("-", charData);
                        continue;
                    }

                    var encounter = charData.Encounters.FirstOrDefault(
                        enc => enc.Id == this.currentEncounter.EncounterId && enc.Difficulty == this.currentEncounter.DifficultyId);

                    var (_, hoverMessage) = GetEncounterInfo(encounter, this.currentEncounter, charData);
                    DrawEncounterStat(encounter, stat, hoverMessage);
                }
            }

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
                        ImGui.TableNextColumn();
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
                        ImGui.TableNextColumn();
                        DrawEncounterStat(encounter, stat, hoverMessage);
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
        DrawHeaderSeparator();

        this.DrawSwapAlias(entry, entry.Alias, row);

        DrawHeaderSeparator();
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

    private void ShiftCurrentStat(int shift)
    {
        var enabledStats = Service.Configuration.Stats.Where(stat => stat.IsEnabled).ToList();
        if (enabledStats.Count == 0)
        {
            return;
        }

        var currIndex = enabledStats.IndexOf(this.currentStat);
        var newIndex = Util.MathMod(currIndex + shift, enabledStats.Count);
        if (newIndex < enabledStats.Count)
        {
            this.currentStat = enabledStats[newIndex];
        }
    }

    private void ShiftCurrentEncounter(int shift)
    {
        var displayedEncounters = this.GetDisplayedEntries().Where(encounter => encounter.Type == LayoutEntryType.Encounter).ToList();
        if (displayedEncounters.Count == 0)
        {
            return;
        }

        var currIndex = displayedEncounters.IndexOf(this.currentEncounter);
        var newIndex = Util.MathMod(currIndex + shift, displayedEncounters.Count);
        if (newIndex < displayedEncounters.Count)
        {
            this.currentEncounter = displayedEncounters[newIndex];
        }
    }
}
