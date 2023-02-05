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

    public unsafe void UpdateTeamList()
    {
        this.TeamList = new List<TeamMember>();

        var groupManager = GroupManager.Instance();
        if (groupManager->MemberCount > 0)
        {
            this.AddMembersFromGroupManager(groupManager);
        }
        else
        {
            var cwProxy = InfoProxyCrossRealm.Instance();
            if (cwProxy->IsInCrossRealmParty != 0)
            {
                var localIndex = cwProxy->LocalPlayerGroupIndex;
                this.AddMembersFromCRGroup(cwProxy->CrossRealmGroupSpan[localIndex], true);

                for (var i = 0; i < cwProxy->CrossRealmGroupSpan.Length; i++)
                {
                    if (i == localIndex)
                    {
                        continue;
                    }

                    this.AddMembersFromCRGroup(cwProxy->CrossRealmGroupSpan[i]);
                }
            }
        }

        // Add self if not in party
        if (this.TeamList.Count == 0 && Service.ClientState.LocalPlayer != null)
        {
            var selfName = Service.ClientState.LocalPlayer.Name.TextValue;
            var selfWorldId = Service.ClientState.LocalPlayer.HomeWorld.Id;
            var selfJobId = Service.ClientState.LocalPlayer.ClassJob.Id;
            this.AddTeamMember(selfName, (ushort)selfWorldId, selfJobId, true);
        }
    }

    private unsafe void AddMembersFromCRGroup(CrossRealmGroup crossRealmGroup, bool isLocalPlayerGroup = false)
    {
        foreach (var groupMember in crossRealmGroup.GroupMemberSpan)
        {
            this.AddTeamMember(Util.ReadSeString(groupMember.Name).TextValue, (ushort)groupMember.HomeWorld, groupMember.ClassJobId, isLocalPlayerGroup);
        }
    }

    private unsafe void AddMembersFromGroupManager(GroupManager* groupManager)
    {
        for (var i = 0; i < groupManager->MemberCount; i++)
        {
            var partyMember = groupManager->GetPartyMemberByIndex(i);
            if (partyMember != null)
            {
                this.AddTeamMember(Util.ReadSeString(partyMember->Name).TextValue, partyMember->HomeWorld, partyMember->ClassJob, true);
            }
        }

        for (var i = 0; i < 20; i++)
        {
            var allianceMember = groupManager->GetAllianceMemberByIndex(i);
            if (allianceMember != null)
            {
                this.AddTeamMember(Util.ReadSeString(allianceMember->Name).TextValue, allianceMember->HomeWorld, allianceMember->ClassJob, false);
            }
        }
    }

    private void AddTeamMember(string fullName, ushort worldId, uint jobId, bool isInParty)
    {
        var world = Service.DataManager.GetExcelSheet<World>()?.FirstOrDefault(x => x.RowId == worldId);
        if (world == null)
        {
            return;
        }

        if (fullName == string.Empty)
        {
            return;
        }

        var splitName = fullName.Split(' ');
        if (splitName.Length != 2)
        {
            return;
        }

        this.TeamList.Add(new TeamMember { FirstName = splitName[0], LastName = splitName[1], World = world.Name, JobId = jobId, IsInParty = isInParty });
    }
}
