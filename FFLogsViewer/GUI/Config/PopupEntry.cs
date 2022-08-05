using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using FFLogsViewer.Model;
using ImGuiNET;
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

            if (!Service.GameDataManager.IsDataReady && !Service.GameDataManager.IsDataLoading && !Service.GameDataManager.HasFailed)
            {
                Service.GameDataManager.FetchData();
            }

            if (currLayoutEntry.Type == LayoutEntryType.Encounter)
            {
                if (!Service.GameDataManager.IsDataReady && Service.GameDataManager.IsDataLoading)
                {
                    ImGui.Text("Fetching data...");
                    ImGui.EndPopup();
                    return;
                }

                if (!Service.GameDataManager.IsDataLoading && Service.GameDataManager.HasFailed)
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

                Service.Configuration.IsDefaultLayout = false;
                Service.Configuration.Save();
                Service.MainWindow.ResetSwapGroups();
            }
            else
            {
                if (!Service.Configuration.Layout[this.SelectedIndex].Compare(currLayoutEntry))
                {
                    Service.Configuration.Layout[this.SelectedIndex] = currLayoutEntry;
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
                                                                             currLayoutEntry.Expansion == layoutEntry.Expansion &&
                                                                             currLayoutEntry.Zone == layoutEntry.Zone &&
                                                                             currLayoutEntry.Encounter == layoutEntry.Encounter &&
                                                                             currLayoutEntry.Difficulty == layoutEntry.Difficulty))
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
