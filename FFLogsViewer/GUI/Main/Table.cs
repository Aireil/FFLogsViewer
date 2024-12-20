using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using FFLogsViewer.Manager;
using FFLogsViewer.Model;
using ImGuiNET;

namespace FFLogsViewer.GUI.Main;

public class Table
{
    private Dictionary<string, int> currSwaps = new();

    private StatType? currentStatType;
    private Stat CurrentStat
    {
        get
        {
            if (this.currentStatType == null)
            {
                if (Service.Configuration.DefaultStatTypePartyView != null)
                {
                    var stat = Service.Configuration.Stats.First(
                        stat => stat.Type == Service.Configuration.DefaultStatTypePartyView);
                    this.currentStatType = stat.Type;
                }

                this.currentStatType ??= Service.Configuration.Stats.First(stat => stat.IsEnabled).Type;
            }

            return Service.Configuration.Stats.First(stat => stat.Type == this.currentStatType);
        }
        set => this.currentStatType = value.Type;
    }

    private LayoutEntry? currentEncounter;
    private LayoutEntry CurrentEncounter
    {
        get
        {
            if (this.currentEncounter == null)
            {
                var defaultEncounter = Service.Configuration.DefaultEncounterPartyView;
                if (defaultEncounter != null)
                {
                    var entry = Service.Configuration.Layout.Find(
                        entry => entry.EncounterId == defaultEncounter.EncounterId && entry.DifficultyId == defaultEncounter.DifficultyId);
                    if (entry != null)
                    {
                        this.currentEncounter = entry;
                    }
                }

                this.currentEncounter ??= Service.Configuration.Layout.First(entry => entry.Type == LayoutEntryType.Encounter);
            }

            return this.currentEncounter;
        }
        set => this.currentEncounter = value;
    }

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
                               "This error is expected when the encounter is a recent addition to the layout or is not yet listed on FF Logs.\n" +
                               "It can also be caused by an issue with FF Logs API, please check if the zone is visible on the character's page on FF Logs' website." +
                               "\n\n" +
                               "If you see it properly on the website, please " +
                               (Service.Configuration.IsDefaultLayout
                                    ? "report the issue on GitHub."
                                    : "try adding the encounter again.")
                               ;
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
                hoverMessage = hoverMessage.Insert(0, $"Zone ASP: {(encounter?.BestAllStarsPointsZone == null ? "-" : $" {encounter.BestAllStarsPointsZone?.ToString("0.00")}")}\n");
                break;
            case StatType.AllStarsRank:
                text = encounter?.AllStarsRank?.ToString();
                color = Util.GetLogColor(encounter?.AllStarsRankPercent);
                hoverMessage = hoverMessage.Insert(0, $"Zone ASP R: {(encounter?.BestAllStarsRankZone == null ? "-" : $"{encounter.BestAllStarsRankZone}")}\n");
                break;
            case StatType.AllStarsRankPercent:
                text = Util.GetFormattedLog(encounter?.AllStarsRankPercent, Service.Configuration.NbOfDecimalDigits);
                color = Util.GetLogColor(encounter?.AllStarsRankPercent);
                hoverMessage = hoverMessage.Insert(0, $"Zone ASP R%: {(encounter?.BestAllStarsRankPercentZone == null ? "-" : $"{Util.GetFormattedLog(encounter.BestAllStarsRankPercentZone, Service.Configuration.NbOfDecimalDigits)}")}\n");
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

    private static void DrawPartyViewWarning()
    {
        if (Service.CharDataManager.PartyMembers.Count == 0)
        {
            ImGui.Text("Use");
            using var font = ImRaii.PushFont(UiBuilder.IconFont);
            ImGui.SameLine();
            ImGui.Text(FontAwesomeIcon.Redo.ToIconString());
            font.Pop();
            ImGui.SameLine();
            ImGui.Text("to refresh the party state.");
        }
    }

    private void DrawPartyView()
    {
        var configErrorMessage = Service.Configuration.Layout.Count == 0 ? "You have no layout set up." : null;
        configErrorMessage ??= Service.Configuration.Stats.Any(stat => stat.IsEnabled) ? null : "You have no stat enabled.";
        if (configErrorMessage != null)
        {
            if (Util.CenterSelectable($"{configErrorMessage} Click to open settings."))
            {
                Service.ConfigWindow.IsOpen = true;
            }

            return;
        }

        if (Util.DrawButtonIcon(FontAwesomeIcon.Redo))
        {
            Service.CharDataManager.UpdatePartyMembers();
        }

        Util.SetHoverTooltip("Refresh party state");

        ImGui.SameLine();
        if (Util.DrawButtonIcon(FontAwesomeIcon.ExchangeAlt))
        {
            Service.Configuration.IsEncounterLayout = !Service.Configuration.IsEncounterLayout;
            Service.Configuration.Save();
        }

        Util.SetHoverTooltip(Service.Configuration.IsEncounterLayout ? "Swap to stat layout" : "Swap to encounter layout");

        ImGui.SameLine();
        if (Util.DrawButtonIcon(FontAwesomeIcon.Star))
        {
            if (Service.Configuration.IsEncounterLayout)
            {
                Service.Configuration.DefaultStatTypePartyView = this.CurrentStat.Type;
            }
            else
            {
                Service.Configuration.DefaultEncounterPartyView = this.CurrentEncounter;
            }

            Service.Configuration.Save();
        }

        Util.SetHoverTooltip($"Set the current {(Service.Configuration.IsEncounterLayout ? "stat" : "encounter")} as default");

        if (Service.Configuration.IsEncounterLayout)
        {
            this.DrawEncounterLayout();
        }
        else
        {
            this.DrawStatLayout();
        }
    }

    private void DrawEncounterLayout()
    {
        var currentParty = Service.CharDataManager.PartyMembers;
        var displayedEntries = this.GetDisplayedEntries();

        this.DrawEncounterHeader();

        if (ImGui.BeginTable(
                "##MainWindowTablePartyViewEncounterLayout",
                Service.Configuration.Style.IsLocalPlayerInPartyView ? 9 : 8,
                Service.Configuration.Style.MainTableFlags))
        {
            ImGui.TableNextColumn();

            var separatorY = ImGui.GetCursorPosY();
            if (Service.Configuration.Style.IsHeaderSeparatorDrawn && displayedEntries[0].Type != LayoutEntryType.Header)
            {
                ImGui.Separator();
            }

            for (var i = 0; i < (Service.Configuration.Style.IsLocalPlayerInPartyView ? 8 : 7); i++)
            {
                var charData = i < currentParty.Count ? currentParty[i] : null;

                ImGui.TableNextColumn();

                var iconSize = Util.Round(25 * ImGuiHelpers.GlobalScale);
                Util.CenterCursor(iconSize);
                ImGui.Image(Service.TextureProvider.GetFromGameIcon(new GameIconLookup(Util.GetJobIconId(charData?.JobId ?? 0))).GetWrapOrEmpty().ImGuiHandle, new Vector2(iconSize));

                if (charData != null)
                {
                    var jobColor = Service.MainWindow.Job.Color;
                    if (Service.MainWindow.Job.Name != "All jobs")
                    {
                        jobColor = GameDataManager.Jobs.FirstOrDefault(job => job.Id == charData.LoadedJobId)?.Color ?? jobColor;
                    }

                    using var color = ImRaii.PushColor(ImGuiCol.Text, jobColor);
                    Util.CenterSelectableWithError(charData.Abbreviation + $"##Selectable{i}", charData);
                    Util.LinkOpenOrPopup(charData);

                    color.Pop();

                    if (charData.CharError == null)
                    {
                        Util.SetHoverTooltip($"{charData.FirstName} {charData.LastName}@{charData.WorldName}");
                    }
                }
                else
                {
                    Util.CenterText("-");
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

                    for (var i = 0; i < (Service.Configuration.Style.IsLocalPlayerInPartyView ? 8 : 7); i++)
                    {
                        ImGui.TableNextColumn();
                        var charData = i < currentParty.Count ? currentParty[i] : null;
                        DrawStatHeader(this.CurrentStat, charData);
                    }
                }
                else if (entry.Type == LayoutEntryType.Encounter)
                {
                    this.DrawEncounterName(entry, entry.Alias == string.Empty ? entry.Encounter : entry.Alias, string.Empty, row);

                    for (var i = 0; i < (Service.Configuration.Style.IsLocalPlayerInPartyView ? 8 : 7); i++)
                    {
                        ImGui.TableNextColumn();
                        var charData = i < currentParty.Count ? currentParty[i] : null;
                        if (charData == null)
                        {
                            Util.CenterText("-");
                            continue;
                        }

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
                        DrawEncounterStat(encounter, this.CurrentStat, hoverMessage);
                    }
                }
            }

            ImGui.EndTable();
        }
    }

    private void DrawEncounterHeader()
    {
        ImGui.SameLine();
        var metricAbbreviation = Util.GetMetricAbbreviation(Service.CharDataManager.PartyMembers.FirstOrDefault());
        var comboSize = Util.Round(Service.Configuration.Stats.Where(stat => stat.IsEnabled).Select(metric => ImGui.CalcTextSize(metric.GetFinalAlias(metricAbbreviation)).X).Max()
                        + (30 * ImGuiHelpers.GlobalScale)
                        + ImGui.CalcTextSize(" (★)").X);
        ImGui.SetNextItemWidth(comboSize);

        var comboPreview = this.CurrentStat.GetFinalAlias(metricAbbreviation);
        if (Service.Configuration.DefaultStatTypePartyView == this.CurrentStat.Type)
        {
            comboPreview += " (★)";
        }

        if (ImGui.BeginCombo("##EncounterLayoutCombo", comboPreview, ImGuiComboFlags.HeightLargest))
        {
            foreach (var stat in Service.Configuration.Stats.Where(stat => stat.IsEnabled))
            {
                var statName = stat.Name;
                if (Service.Configuration.DefaultStatTypePartyView == stat.Type)
                {
                    statName += " (★)";
                }

                if (ImGui.Selectable(statName))
                {
                    this.CurrentStat = stat;
                }
            }

            ImGui.EndCombo();
        }

        this.DrawPartyViewArrows();
        DrawPartyViewWarning();
    }

    private void DrawStatLayoutHeader()
    {
        var encounterAbbreviation = this.CurrentEncounter.Alias != string.Empty
                                        ? this.CurrentEncounter.Alias
                                        : this.CurrentEncounter.Encounter;

        if (this.CurrentEncounter.EncounterId == Service.Configuration.DefaultEncounterPartyView?.EncounterId
            && this.CurrentEncounter.DifficultyId == Service.Configuration.DefaultEncounterPartyView?.DifficultyId)
        {
            encounterAbbreviation += " (★)";
        }

        ImGui.SameLine();
        var comboSize = Util.Round(Service.Configuration.Layout.Select(entry => ImGui.CalcTextSize(entry.Alias != string.Empty ? entry.Alias : entry.Encounter).X).Max()
                        + (30 * ImGuiHelpers.GlobalScale)
                        + ImGui.CalcTextSize(" (★)").X);
        ImGui.SetNextItemWidth(comboSize);
        if (ImGui.BeginCombo("##StatLayoutCombo", encounterAbbreviation, ImGuiComboFlags.HeightLargest))
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
                    var encounterName = entry.Encounter;
                    if (entry.EncounterId == Service.Configuration.DefaultEncounterPartyView?.EncounterId
                        && entry.DifficultyId == Service.Configuration.DefaultEncounterPartyView?.DifficultyId)
                    {
                        encounterName += " (★)";
                    }

                    if (ImGui.Selectable($"{encounterName}##{i}"))
                    {
                        this.currentEncounter = entry;
                    }
                }
            }

            ImGui.EndCombo();
        }

        this.DrawPartyViewArrows();
        DrawPartyViewWarning();
    }

    private void DrawPartyViewArrows()
    {
        var arrowSize = Util.Round(3 * ImGuiHelpers.GlobalScale);

        ImGui.SameLine();
        if (Util.DrawButtonIcon(FontAwesomeIcon.ArrowLeft, new Vector2(arrowSize)))
        {
            this.ShiftCurrentLayout(-1);
        }

        ImGui.SameLine();
        if (Util.DrawButtonIcon(FontAwesomeIcon.ArrowRight, new Vector2(arrowSize)))
        {
            this.ShiftCurrentLayout(1);
        }
    }

    private void DrawStatLayout()
    {
        var currentParty = Service.CharDataManager.PartyMembers;
        var enabledStats = Service.Configuration.Stats.Where(stat => stat.IsEnabled).ToList();

        this.DrawStatLayoutHeader();

        if (ImGui.BeginTable(
                "##MainWindowTablePartyViewStatLayout",
                enabledStats.Count + 1,
                Service.Configuration.Style.MainTableFlags))
        {
            ImGui.TableNextColumn();

            var separatorY = ImGui.GetCursorPosY() + ImGui.GetFontSize() + ImGui.GetStyle().ItemSpacing.Y;
            ImGui.SetCursorPosY(separatorY);
            if (Service.Configuration.Style.IsHeaderSeparatorDrawn)
            {
                ImGui.Separator();
            }

            foreach (var stat in enabledStats)
            {
                ImGui.TableNextColumn();
                DrawStatHeader(stat, currentParty.Count > 0 ? currentParty[0] : null, false);

                if (Service.Configuration.Style.IsHeaderSeparatorDrawn)
                {
                    ImGui.SetCursorPosY(separatorY);
                    ImGui.Separator();
                }
            }

            for (var i = 0; i < (Service.Configuration.Style.IsLocalPlayerInPartyView ? 8 : 7); i++)
            {
                var charData = i < currentParty.Count ? currentParty[i] : null;

                if (i != 0)
                {
                    ImGui.TableNextRow();
                }

                ImGui.TableNextColumn();
                var iconSize = Util.Round(25 * ImGuiHelpers.GlobalScale);
                var middleCursorPosY = ImGui.GetCursorPosY() + (iconSize / 2) - (ImGui.GetFontSize() / 2);
                ImGui.Image(Service.TextureProvider.GetFromGameIcon(new GameIconLookup(Util.GetJobIconId(charData?.JobId ?? 0))).GetWrapOrEmpty().ImGuiHandle, new Vector2(iconSize));

                ImGui.SameLine();
                ImGui.SetCursorPosY(middleCursorPosY);
                if (charData != null)
                {
                    var jobColor = Service.MainWindow.Job.Color;
                    if (Service.MainWindow.Job.Name != "All jobs")
                    {
                        jobColor = GameDataManager.Jobs.FirstOrDefault(job => job.Id == charData.LoadedJobId)?.Color ?? jobColor;
                    }

                    using var color = ImRaii.PushColor(ImGuiCol.Text, jobColor);
                    Util.SelectableWithError($"{charData.FirstName} {charData.LastName}##Selectable{i}", charData);
                    Util.LinkOpenOrPopup(charData);

                    color.Pop();

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

                    if (charData == null)
                    {
                        Util.CenterText("-");
                        continue;
                    }

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
                        enc => enc.Id == this.CurrentEncounter.EncounterId && enc.Difficulty == this.CurrentEncounter.DifficultyId);

                    var (_, hoverMessage) = GetEncounterInfo(encounter, this.CurrentEncounter, charData);
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
        ImRaii.Color color = new();
        if (!hoverMessage.IsNullOrEmpty())
        {
            color.Push(ImGuiCol.Text, ImGuiColors.DalamudGrey);
        }

        this.DrawSwapAlias(entry, encounterName, row);

        if (!hoverMessage.IsNullOrEmpty())
        {
            color.Pop();
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
            //  == mouse character from game's font
            if (ImGui.Selectable($"{displayedName} {(entry.Type == LayoutEntryType.Header ? "" : string.Empty)}##{row}"))
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

        var currIndex = enabledStats.IndexOf(this.CurrentStat);
        var newIndex = Util.MathMod(currIndex + shift, enabledStats.Count);
        if (newIndex < enabledStats.Count)
        {
            this.CurrentStat = enabledStats[newIndex];
        }
    }

    private void ShiftCurrentEncounter(int shift)
    {
        var displayedEncounters = Service.Configuration.Layout.Where(encounter => encounter.Type == LayoutEntryType.Encounter).ToList();
        if (displayedEncounters.Count == 0)
        {
            return;
        }

        var currIndex = displayedEncounters.IndexOf(this.CurrentEncounter);
        var newIndex = Util.MathMod(currIndex + shift, displayedEncounters.Count);
        if (newIndex < displayedEncounters.Count)
        {
            this.currentEncounter = displayedEncounters[newIndex];
        }
    }

    private void ShiftCurrentLayout(int shift)
    {
        if (Service.Configuration.IsEncounterLayout)
        {
            this.ShiftCurrentStat(shift);
        }
        else
        {
            this.ShiftCurrentEncounter(shift);
        }
    }
}
