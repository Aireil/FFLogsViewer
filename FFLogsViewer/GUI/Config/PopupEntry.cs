using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using FFLogsViewer.Model;

using Encounter = FFLogsViewer.Model.GameData.Encounter;

namespace FFLogsViewer.GUI.Config;

public class PopupEntry
{
    public enum Mode
    {
        Adding,
        Editing,
    }

    private LayoutEntry AddLayoutEntry { get; set; } = LayoutEntry.CreateEncounter();
    private LayoutEntry EditLayoutEntry { get; set; } = null!;
    private List<Encounter> AllEncounters { get; set; } = null!;

    public int SelectedIndex;
    private const string AllEncountersPlaceholder = "All encounters##AllCheck";
    private Mode mode = Mode.Adding;
    private bool hasDeleted;

    public void Open()
    {
        if (this.mode == Mode.Adding)
        {
            this.AddLayoutEntry.Alias = string.Empty;
        }
        else if (this.mode == Mode.Editing)
        {
            this.EditLayoutEntry = (LayoutEntry)Service.Configuration.Layout[this.SelectedIndex].Clone();
        }

        ImGui.OpenPopup("##PopupEntry");
    }

    public void SwitchMode(Mode popupMode)
    {
        this.mode = popupMode;
    }

    public void Draw()
    {
        if (ImGui.BeginPopup("##PopupEntry"))
        {
            var currLayoutEntry = this.mode == Mode.Adding ? this.AddLayoutEntry : this.EditLayoutEntry;

            var tmpLayoutEntryType = currLayoutEntry.Type;
            if (ImGui.RadioButton("Encounter", tmpLayoutEntryType == LayoutEntryType.Encounter))
            {
                currLayoutEntry.Type = LayoutEntryType.Encounter;
            }

            ImGui.SameLine();
            if (ImGui.RadioButton("Header", tmpLayoutEntryType == LayoutEntryType.Header))
            {
                currLayoutEntry.Type = LayoutEntryType.Header;
            }

            Util.DrawHelp("A header displays the stat for each column.");

            var alias = currLayoutEntry.Alias;
            if (ImGui.InputText("Alias", ref alias, 400))
            {
                currLayoutEntry.Alias = alias;
            }

            Util.DrawHelp("Optional, will overwrite the encounter name from FF Logs");

            if (Service.GameDataManager is { IsDataReady: false, IsDataLoading: false, HasFailed: false })
            {
                Service.GameDataManager.FetchData();
            }

            if (currLayoutEntry.Type == LayoutEntryType.Encounter)
            {
                if (Service.GameDataManager is { IsDataReady: false, IsDataLoading: true })
                {
                    ImGui.Text("Fetching data...");
                    ImGui.EndPopup();
                    return;
                }

                if (Service.GameDataManager is { IsDataLoading: false, HasFailed: true })
                {
                    if (ImGui.Button("Couldn't fetch data, try again?"))
                    {
                        Service.GameDataManager.HasFailed = false;
                    }

                    ImGui.EndPopup();
                    return;
                }

                this.DrawEntryEncounter(currLayoutEntry);
            }
            else
            {
                this.DrawEntryHeader(currLayoutEntry);
            }

            if (this.hasDeleted)
            {
                this.hasDeleted = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    private static void DrawEntrySwap(LayoutEntry currLayoutEntry)
    {
        const string helpMessage =
            "Optional, setting a Swap ID/# group allows you to click these encounters/headers\n" +
            "in the main window to dynamically change the layout.\n" +
            "All groups are reset to the lowest Swap # after a restart\n" +
            "Note: Data is still fetched even if not displayed.\n" +
            "\n" +
            "Swap ID: ID of the Swap ID/# group.\n" +
            "Swap #: Order of the swaps in the group, smallest value is the default.\n";

        var swapId = currLayoutEntry.SwapId;
        if (ImGui.InputText("Swap ID", ref swapId, 400))
        {
            currLayoutEntry.SwapId = swapId;
        }

        Util.DrawHelp(helpMessage);

        if (currLayoutEntry.SwapId == string.Empty)
        {
            ImGui.BeginDisabled();
        }

        var swapNumber = currLayoutEntry.SwapNumber;
        if (ImGui.InputInt("Swap #", ref swapNumber))
        {
            currLayoutEntry.SwapNumber = swapNumber;
        }

        if (currLayoutEntry.SwapId == string.Empty)
        {
            ImGui.EndDisabled();
        }

        Util.DrawHelp(helpMessage);
    }

    private static void RefreshForceADPSStates(int zoneId, bool isForcingADPS)
    {
        foreach (var entry in Service.Configuration.Layout)
        {
            if (entry.ZoneId == zoneId)
            {
                entry.IsForcingADPS = isForcingADPS;
            }
        }

        var zoneIdsWithIsForcingADPS = Service.Configuration.Layout
                                          .GroupBy(entry => entry.ZoneId)
                                          .Where(group => group.Any(entry => entry.IsForcingADPS))
                                          .Select(group => group.Key)
                                          .ToList();

        foreach (var entry in Service.Configuration.Layout)
        {
            entry.IsForcingADPS = zoneIdsWithIsForcingADPS.Contains(entry.ZoneId);
        }
    }

    private void DrawEntryEncounter(LayoutEntry currLayoutEntry)
    {
        var expansions = Service.GameDataManager.GameData!.Data!.WorldData!.Expansions!;
        if (ImGui.BeginCombo("Expansion##PopupEntryExpansion", currLayoutEntry.Expansion))
        {
            for (var i = 0; i < expansions.Count; i++)
            {
                if (ImGui.Selectable($"{expansions[i].Name}##{i}"))
                {
                    currLayoutEntry.Expansion = expansions[i].Name!;
                    currLayoutEntry.Zone = "-";
                    currLayoutEntry.ZoneId = 0;
                    currLayoutEntry.Encounter = "-";
                    currLayoutEntry.EncounterId = 0;
                    currLayoutEntry.Difficulty = "-";
                    currLayoutEntry.DifficultyId = 0;
                    currLayoutEntry.IsForcingADPS = false;
                }
            }

            ImGui.EndCombo();
        }

        var zones = expansions!.FirstOrDefault(expansion => expansion.Name == currLayoutEntry.Expansion)?.Zones;
        if (ImGui.BeginCombo("Zone##PopupEntryZone", currLayoutEntry.Zone))
        {
            if (zones is { Count: > 0 })
            {
                for (var i = 0; i < zones.Count; i++)
                {
                    if (ImGui.Selectable($"{zones[i].Name}##{i}"))
                    {
                        currLayoutEntry.Zone = zones[i].Name!;
                        currLayoutEntry.ZoneId = zones[i].Id!.Value;
                        currLayoutEntry.Encounter = "-";
                        currLayoutEntry.EncounterId = 0;
                        currLayoutEntry.Difficulty = "-";
                        currLayoutEntry.DifficultyId = 0;
                        currLayoutEntry.IsForcingADPS = false;
                    }
                }
            }
            else
            {
                ImGui.Selectable("No zone found for this expansion.", true, ImGuiSelectableFlags.Disabled);
            }

            ImGui.EndCombo();
        }

        var encounters = zones?.FirstOrDefault(zone => zone.Name == currLayoutEntry.Zone)?.Encounters;
        if (ImGui.BeginCombo("Encounter##PopupEntryEncounter", currLayoutEntry.Encounter))
        {
            if (encounters is { Count: > 0 })
            {
                if (this.mode == Mode.Adding && encounters.Count > 1)
                {
                    if (ImGui.Selectable($"All encounters in {currLayoutEntry.Zone}"))
                    {
                        currLayoutEntry.Encounter = AllEncountersPlaceholder;
                        this.AllEncounters = encounters;
                    }

                    ImGui.Separator();
                }

                for (var i = 0; i < encounters.Count; i++)
                {
                    if (ImGui.Selectable($"{encounters[i].Name}##{i}"))
                    {
                        currLayoutEntry.Encounter = encounters[i].Name!;
                        currLayoutEntry.EncounterId = encounters[i].Id!.Value;
                    }
                }
            }
            else
            {
                ImGui.Selectable("No encounter found for this zone.", true, ImGuiSelectableFlags.Disabled);
            }

            ImGui.EndCombo();
        }

        var difficulties = zones?.FirstOrDefault(zone => zone.Name == currLayoutEntry.Zone)?.Difficulties;
        if (difficulties != null && difficulties.Count != 0)
        {
            if (difficulties.Count == 1)
            {
                currLayoutEntry.Difficulty = difficulties[0].Name!;
                currLayoutEntry.DifficultyId = difficulties[0].Id!.Value;
            }
            else
            {
                if (ImGui.BeginCombo("Difficulty##PopupEntryDifficulty", currLayoutEntry.Difficulty))
                {
                    for (var i = 0; i < difficulties.Count; i++)
                    {
                        if (ImGui.Selectable($"{difficulties[i].Name}##{i}"))
                        {
                            currLayoutEntry.Difficulty = difficulties[i].Name!;
                            currLayoutEntry.DifficultyId = difficulties[i].Id!.Value;
                        }
                    }

                    ImGui.EndCombo();
                }
            }
        }

        DrawEntrySwap(currLayoutEntry);

        var isForcingADPS = currLayoutEntry.IsForcingADPS;
        if (ImGui.Checkbox("Force aDPS", ref isForcingADPS))
        {
            currLayoutEntry.IsForcingADPS = !currLayoutEntry.IsForcingADPS;
        }

        Util.DrawHelp($"Use aDPS instead of your default metric ({Service.Configuration.Metric.Name}) on that entire zone," +
                      "\nthis is only useful for zones where your default metric is not available." +
                      "\nThere will be no feedback in the main window table, headers will still show the default." +
                      "\nIf you are unsure, leave this unchecked.");

        if (currLayoutEntry.IsForcingADPS)
        {
            ImGui.SameLine();
            ImGui.TextColored(ImGuiColors.DalamudRed, "-> Hover this text and read <-");
            Util.SetHoverTooltip("Please understand the consequences of forcing aDPS:" +
                                 "\n - It will only force when your default metric is active." +
                                 "\n - It affects all the encounters in this zone." +
                                 "\n - There will be NO way of telling in the main window." +
                                 "\n - Headers will only show your default metric." +
                                 "\n" +
                                 "\nThis is only useful for zones where your default metric is not available." +
                                 "\nIf you are unsure, uncheck this setting.");
        }

        if (!currLayoutEntry.IsEncounterValid())
        {
            ImGui.BeginDisabled();
        }

        if (ImGui.Button(this.mode == Mode.Adding ? "Add" : "Edit"))
        {
            if (this.mode == Mode.Adding)
            {
                var newEntries = new List<LayoutEntry>();
                if (currLayoutEntry.Encounter == AllEncountersPlaceholder)
                {
                    foreach (var encounter in this.AllEncounters)
                    {
                        var entry = (LayoutEntry)currLayoutEntry.Clone();
                        entry.Encounter = encounter.Name!;
                        entry.EncounterId = encounter.Id!.Value;
                        newEntries.Add(entry);
                    }
                }
                else
                {
                    newEntries.Add((LayoutEntry)currLayoutEntry.Clone());
                }

                if (this.SelectedIndex >= 0)
                {
                    Service.Configuration.Layout.InsertRange(this.SelectedIndex, newEntries);
                }
                else
                {
                    Service.Configuration.Layout.AddRange(newEntries);
                }

                RefreshForceADPSStates(currLayoutEntry.ZoneId, currLayoutEntry.IsForcingADPS);
                Service.Configuration.IsDefaultLayout = false;
                Service.Configuration.Save();
                Service.MainWindow.ResetSwapGroups();
            }
            else
            {
                if (!Service.Configuration.Layout[this.SelectedIndex].Compare(currLayoutEntry))
                {
                    Service.Configuration.Layout[this.SelectedIndex] = currLayoutEntry;
                    RefreshForceADPSStates(currLayoutEntry.ZoneId, currLayoutEntry.IsForcingADPS);
                    Service.Configuration.IsDefaultLayout = false;
                    Service.Configuration.Save();
                    Service.MainWindow.ResetSwapGroups();
                }
            }

            ImGui.CloseCurrentPopup();
        }

        if (!currLayoutEntry.IsEncounterValid())
        {
            ImGui.EndDisabled();
            ImGui.SameLine();
            ImGui.TextColored(ImGuiColors.DalamudGrey, "Please select an encounter.");
        }

        if (this.mode == Mode.Adding && Service.Configuration.Layout.Any(layoutEntry => currLayoutEntry.Type != LayoutEntryType.Header &&
                                                                             currLayoutEntry.Type == layoutEntry.Type &&
                                                                             currLayoutEntry.ZoneId == layoutEntry.ZoneId &&
                                                                             currLayoutEntry.DifficultyId == layoutEntry.DifficultyId &&
                                                                             currLayoutEntry.EncounterId == layoutEntry.EncounterId))
        {
            ImGui.SameLine();
            ImGui.Text("Note: this encounter is already in the layout.");
        }

        if (this.mode == Mode.Editing)
        {
            this.DrawDeleteButton();
        }
    }

    private void DrawEntryHeader(LayoutEntry currLayoutEntry)
    {
        DrawEntrySwap(currLayoutEntry);

        if (ImGui.Button(this.mode == Mode.Adding ? "Add" : "Edit"))
        {
            var newLayoutEntry = currLayoutEntry.CloneHeader();

            if (this.mode == Mode.Adding)
            {
                if (this.SelectedIndex >= 0)
                {
                    Service.Configuration.Layout.Insert(this.SelectedIndex, newLayoutEntry);
                }
                else
                {
                    Service.Configuration.Layout.Add(newLayoutEntry);
                }

                Service.Configuration.IsDefaultLayout = false;
                Service.Configuration.Save();
                Service.MainWindow.ResetSwapGroups();
            }
            else
            {
                if (!Service.Configuration.Layout[this.SelectedIndex].Compare(newLayoutEntry))
                {
                    Service.Configuration.Layout[this.SelectedIndex] = newLayoutEntry;
                    Service.Configuration.IsDefaultLayout = false;
                    Service.Configuration.Save();
                    Service.MainWindow.ResetSwapGroups();
                }
            }

            ImGui.CloseCurrentPopup();
        }

        if (this.mode == Mode.Editing)
        {
            this.DrawDeleteButton();
        }
    }

    private void DrawDeleteButton()
    {
        ImGui.SameLine();
        if (Util.DrawButtonIcon(FontAwesomeIcon.Trash, new Vector2(2, ImGui.GetStyle().FramePadding.Y)))
        {
            ImGui.OpenPopup("##Trash");
        }

        if (ImGui.BeginPopup("##Trash", ImGuiWindowFlags.NoMove))
        {
            if (Util.DrawButtonIcon(FontAwesomeIcon.Trash, new Vector2(2, ImGui.GetStyle().FramePadding.Y)))
            {
                Service.Configuration.Layout.RemoveAt(this.SelectedIndex);
                Service.Configuration.IsDefaultLayout = false;
                Service.Configuration.Save();
                Service.MainWindow.ResetSwapGroups();

                this.hasDeleted = true;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }
}
