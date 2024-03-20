using System;
using System.Collections.Generic;
using System.Linq;
using FFLogsViewer.Model;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Lumina.Excel.GeneratedSheets;

namespace FFLogsViewer.Manager;

public class CharDataManager
{
    public CharData DisplayedChar = new();
    public List<CharData> PartyMembers = new();
    public string[] ValidWorlds;

    public void UpdatePartyMembers(bool onlyFetchNewMembers = false)
    {
        Service.TeamManager.UpdateTeamList();
        var localPLayer = Service.ClientState.LocalPlayer;
        var currPartyMembers = Service.TeamManager.TeamList.Where(teamMember => teamMember.IsInParty).ToList();

        if (!Service.Configuration.Style.IsLocalPlayerInPartyView)
        {
            var index = currPartyMembers.FindIndex(member => $"{member.FirstName} {member.LastName}" == localPLayer?.Name.TextValue
                                                             && member.World == localPLayer.HomeWorld.GameData?.Name.RawString);
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

        this.FetchLogs(onlyFetchNewMembers);
    }

    public CharDataManager()
    {
        var worlds = Service.DataManager.GetExcelSheet<World>()?.Where(world => world.IsPublic && world.DataCenter?.Value?.Region != 0);
        if (worlds == null)
        {
            throw new InvalidOperationException("Sheets weren't ready.");
        }

        this.ValidWorlds = worlds.Select(world => world.Name.RawString).ToArray();
    }

    public static string? GetRegionName(string worldName)
    {
        var world = Service.DataManager.GetExcelSheet<World>()?.FirstOrDefault(x => x.Name.ToString().Equals(worldName, StringComparison.InvariantCultureIgnoreCase));
        if (world is not { IsPublic: true })
        {
            return null;
        }

        return world.DataCenter?.Value?.Region switch
        {
            1 => "JP",
            2 => "NA",
            3 => "EU",
            4 => "OC",
            _ => null,
        };
    }

    public static unsafe string? FindPlaceholder(string text)
    {
        try
        {
            var placeholder = Framework.Instance()->GetUiModule()->GetPronounModule()->ResolvePlaceholder(text, 0, 0);
            if (placeholder != null && placeholder->IsCharacter())
            {
                var character = (Character*)placeholder;
                var world = Service.DataManager.GetExcelSheet<World>()
                                   ?.FirstOrDefault(x => x.RowId == character->HomeWorld);

                if (world is { IsPublic: true } && placeholder->Name != null)
                {
                    var name = $"{Util.ReadSeString(placeholder->Name)}@{world.Name}";
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

    public void FetchLogs(bool onlyFetchNewMembers = false)
    {
        if (Service.MainWindow.IsPartyView)
        {
            foreach (var partyMember in this.PartyMembers)
            {
                if (partyMember.IsInfoSet() && (!onlyFetchNewMembers
                                                || (!partyMember.IsDataReady
                                                    && (partyMember.CharError == null
                                                        || (partyMember.CharError != CharacterError.CharacterNotFoundFFLogs
                                                            && partyMember.CharError != CharacterError.HiddenLogs)))))
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
        }
        else
        {
            this.DisplayedChar = new CharData();
        }
    }
}
