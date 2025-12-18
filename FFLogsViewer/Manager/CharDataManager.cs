using System;
using System.Collections.Generic;
using System.Linq;
using FFLogsViewer.Model;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Lumina.Excel.Sheets;

namespace FFLogsViewer.Manager;

public class CharDataManager
{
    public CharData DisplayedChar = new();
    public List<CharData> PartyMembers = [];
    public bool IsCurrPartyAnAlliance;
    public string[] ValidWorlds;

    private uint? currentAllianceIndex;

    public void UpdatePartyMembers(bool forceLocalPlayerParty = true)
    {
        if (forceLocalPlayerParty)
        {
            this.currentAllianceIndex = null;
        }

        Service.TeamManager.UpdateTeamList();
        var localPLayer = Service.PlayerState;
        var currPartyMembers = Service.TeamManager.TeamList.Where(teamMember => teamMember.AllianceIndex == this.currentAllianceIndex).ToList();
        this.IsCurrPartyAnAlliance = this.currentAllianceIndex != null;

        // the alliance is empty, force local player party
        if (this.IsCurrPartyAnAlliance && currPartyMembers.Count == 0)
        {
            this.currentAllianceIndex = null; // not needed, but just in case, careful when touching the code in this method /!\

            // ReSharper disable once TailRecursiveCall
            // ReSharper disable once RedundantArgumentDefaultValue
            this.UpdatePartyMembers(true);
            return;
        }

        if (!Service.Configuration.Style.IsLocalPlayerInPartyView && !this.IsCurrPartyAnAlliance)
        {
            var index = currPartyMembers.FindIndex(member => $"{member.FirstName} {member.LastName}" == localPLayer?.CharacterName
                                                             && member.World == localPLayer.HomeWorld.ValueNullable?.Name);
            if (index >= 0)
            {
                currPartyMembers.RemoveAt(index);
            }
        }

        foreach (var partyMember in currPartyMembers)
        {
            var member = this.PartyMembers.FirstOrDefault(x => x.FirstName == partyMember.FirstName && x.LastName == partyMember.LastName && x.WorldName == partyMember.World);
            if (member == null)
            {
                // add new member
                this.PartyMembers.Add(new CharData(partyMember.FirstName, partyMember.LastName, partyMember.World, partyMember.JobId));
            }
            else
            {
                // update existing member
                member.JobId = partyMember.JobId;
            }
        }

        // remove members that are no longer in party
        this.PartyMembers.RemoveAll(x => !currPartyMembers.Any(y => y.FirstName == x.FirstName && y.LastName == x.LastName && y.World == x.WorldName));

        this.PartyMembers = this.PartyMembers.OrderBy(
            charData => currPartyMembers.FindIndex(
                member => member.FirstName == charData.FirstName &&
                                member.LastName == charData.LastName &&
                                member.World == charData.WorldName)).ToList();

        this.FetchLogs();
    }

    public CharDataManager()
    {
        var worlds = Service.DataManager.GetExcelSheet<World>().Where(Util.IsWorldValid);
        if (worlds == null)
        {
            throw new InvalidOperationException("Sheets weren't ready.");
        }

        this.ValidWorlds = worlds.Select(world => world.Name.ToString()).ToArray();
    }

    public static string GetRegionCode(string worldName)
    {
        var worldSheet = Service.DataManager.GetExcelSheet<World>();

        if (!Util.TryGetFirst(
                worldSheet,
                x => x.Name.ToString().Equals(worldName, StringComparison.InvariantCultureIgnoreCase),
                out var world)
            || !Util.IsWorldValid(world))
        {
            return string.Empty;
        }

        return Util.GetRegionCode(world);
    }

    public static unsafe string? FindPlaceholder(string text)
    {
        try
        {
            var placeholder = Framework.Instance()->GetUIModule()->GetPronounModule()->ResolvePlaceholder(text, 0, 0);
            if (placeholder != null && placeholder->IsCharacter())
            {
                var character = (Character*)placeholder;
                var world = Util.GetWorld(character->HomeWorld);
                if (Util.IsWorldValid(world) && !placeholder->Name.IsEmpty)
                {
                    var name = $"{placeholder->NameString}@{world.Name}";
                    return name;
                }
            }
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error(ex, "Error while resolving placeholder.");
            return null;
        }

        return null;
    }

    public static void OpenCharInBrowser(string name)
    {
        var charData = new CharData();
        if (charData.ParseTextForChar(name))
        {
            Util.OpenFFLogsLink(charData);
        }
    }

    public void FetchLogs(bool ignoreErrors = false)
    {
        if (Service.MainWindow.IsPartyView)
        {
            foreach (var partyMember in this.PartyMembers)
            {
                if (partyMember.IsInfoSet() && (ignoreErrors
                                                || partyMember.CharError == null
                                                || (partyMember.CharError != CharacterError.CharacterNotFoundFFLogs
                                                    && partyMember.CharError != CharacterError.HiddenLogs)))
                {
                    partyMember.FetchLogs();
                }
            }
        }
        else
        {
            if (this.DisplayedChar.IsInfoSet())
            {
                this.DisplayedChar.FetchLogs();
            }
        }
    }

    public void Reset()
    {
        if (Service.MainWindow.IsPartyView)
        {
            this.PartyMembers.Clear();
            this.currentAllianceIndex = null;
            this.IsCurrPartyAnAlliance = false;
        }
        else
        {
            this.DisplayedChar = new CharData();
        }
    }

    public void SwapAlliance()
    {
        this.currentAllianceIndex = Service.TeamManager.GetNextAllianceIndex(this.currentAllianceIndex);
        this.UpdatePartyMembers(false);
    }
}
