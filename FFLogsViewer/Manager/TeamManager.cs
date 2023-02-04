using System.Collections.Generic;
using System.Linq;
using FFLogsViewer.Model;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Lumina.Excel.GeneratedSheets;

namespace FFLogsViewer.Manager;

public class TeamManager
{
    public List<TeamMember> TeamList = new();

    public void UpdateTeamList()
    {
        this.TeamList = GetTeamMembers();
    }

    private static unsafe List<TeamMember> GetTeamMembers()
    {
        var teamMembers = new List<TeamMember>();

        var groupManager = GroupManager.Instance();
        if (groupManager->MemberCount > 0)
        {
            AddMembersFromGroupManager(teamMembers, groupManager);
        }
        else
        {
            var cwProxy = InfoProxyCrossRealm.Instance();
            if (cwProxy->IsInCrossRealmParty != 0)
            {
                var localIndex = cwProxy->LocalPlayerGroupIndex;
                AddMembersFromCRGroup(teamMembers, cwProxy->CrossRealmGroupSpan[localIndex], true);

                for (var i = 0; i < cwProxy->CrossRealmGroupSpan.Length; i++)
                {
                    if (i == localIndex)
                    {
                        continue;
                    }

                    AddMembersFromCRGroup(teamMembers, cwProxy->CrossRealmGroupSpan[i]);
                }
            }
        }

        if (teamMembers.Count == 0)
        {
            var selfName = Service.ClientState.LocalPlayer?.Name;
            var selfWorldId = Service.ClientState.LocalPlayer?.HomeWorld.Id;
            var selfWorld = Service.DataManager.GetExcelSheet<World>()
                                   ?.FirstOrDefault(x => x.RowId == selfWorldId);
            var selfJobId = Service.ClientState.LocalPlayer?.ClassJob.Id;
            if (selfName != null && selfWorld != null && selfJobId != null)
            {
                teamMembers.Add(new TeamMember { Name = selfName.ToString(), World = selfWorld.Name, JobId = selfJobId.Value });
            }
        }

        return teamMembers;
    }

    private static unsafe void AddMembersFromCRGroup(ICollection<TeamMember> teamMembers, CrossRealmGroup crossRealmGroup, bool isLocalPlayerGroup = false)
    {
        foreach (var groupMember in crossRealmGroup.GroupMemberSpan)
        {
            var worldId = groupMember.HomeWorld;
            var world = Service.DataManager.GetExcelSheet<World>()
                               ?.FirstOrDefault(x => x.RowId == worldId);
            if (world == null)
            {
                continue;
            }

            var name = Util.ReadSeString(groupMember.Name);
            teamMembers.Add(new TeamMember { Name = name.ToString(), World = world.Name, JobId = groupMember.ClassJobId, IsInParty = isLocalPlayerGroup });
        }
    }

    private static unsafe void AddMembersFromGroupManager(ICollection<TeamMember> teamMembers, GroupManager* groupManager)
    {
        for (var i = 0; i < groupManager->MemberCount; i++)
        {
            var partyMember = groupManager->GetPartyMemberByIndex(i);

            var teamMember = GetTeamMember(partyMember, true);
            if (teamMember != null && teamMember.Name != string.Empty && teamMember.World != string.Empty)
            {
                teamMembers.Add(teamMember);
            }
        }

        for (var i = 0; i < 20; i++)
        {
            var allianceMember = groupManager->GetAllianceMemberByIndex(i);

            var teamMember = GetTeamMember(allianceMember);
            if (teamMember != null && teamMember.Name != string.Empty && teamMember.World != string.Empty)
            {
                teamMembers.Add(teamMember);
            }
        }
    }

    private static unsafe TeamMember? GetTeamMember(PartyMember* partyMember, bool isLocalPlayerParty = false)
    {
        if (partyMember == null)
        {
            return null;
        }

        var worldId = partyMember->HomeWorld;
        var world = Service.DataManager.GetExcelSheet<World>()
                           ?.FirstOrDefault(x => x.RowId == worldId);
        if (world == null)
        {
            return null;
        }

        var name = Util.ReadSeString(partyMember->Name);
        return new TeamMember { Name = name.ToString(), World = world.Name, JobId = partyMember->ClassJob, IsInParty = isLocalPlayerParty };
    }
}
