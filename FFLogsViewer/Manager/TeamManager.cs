using System.Collections.Generic;
using System.Linq;
using Dalamud.Memory;
using FFLogsViewer.Model;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;

namespace FFLogsViewer.Manager;

public class TeamManager
{
    public List<TeamMember> TeamList = [];

    public unsafe void UpdateTeamList()
    {
        this.TeamList = [];

        var groupManager = GroupManager.Instance()->MainGroup;
        if (groupManager.MemberCount > 0)
        {
            this.AddMembersFromGroupManager(groupManager);
        }
        else
        {
            var cwProxy = InfoProxyCrossRealm.Instance();
            if (cwProxy->IsInCrossRealmParty != 0)
            {
                var localIndex = cwProxy->LocalPlayerGroupIndex;
                this.AddMembersFromCRGroup(cwProxy->CrossRealmGroups[localIndex], true);

                for (var i = 0; i < cwProxy->CrossRealmGroups.Length; i++)
                {
                    if (i == localIndex)
                    {
                        continue;
                    }

                    this.AddMembersFromCRGroup(cwProxy->CrossRealmGroups[i]);
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

    private void AddMembersFromCRGroup(CrossRealmGroup crossRealmGroup, bool isLocalPlayerGroup = false)
    {
        for (var i = 0; i < crossRealmGroup.GroupMemberCount; i++)
        {
            var groupMember = crossRealmGroup.GroupMembers[i];
            this.AddTeamMember(groupMember.NameString, (ushort)groupMember.HomeWorld, groupMember.ClassJobId, isLocalPlayerGroup);
        }
    }

    private unsafe void AddMembersFromGroupManager(GroupManager.Group group)
    {
        var partyMemberList = AgentModule.Instance()->GetAgentHUD()->PartyMembers;
        var groupManagerIndexLeft = Enumerable.Range(0, group.MemberCount).ToList();

        for (var i = 0; i < group.MemberCount; i++)
        {
            var hudPartyMember = partyMemberList[i];
            var hudPartyMemberNameRaw = hudPartyMember.Name;
            if (hudPartyMemberNameRaw != null)
            {
                var hudPartyMemberName = MemoryHelper.ReadSeStringNullTerminated((nint)hudPartyMemberNameRaw).TextValue;
                for (var j = 0; j < group.MemberCount; j++)
                {
                    // handle duplicate names from different worlds
                    if (!groupManagerIndexLeft.Contains(j))
                    {
                        continue;
                    }

                    var partyMember = group.GetPartyMemberByIndex(j);
                    if (partyMember != null)
                    {
                        var partyMemberName = partyMember->NameString;
                        if (hudPartyMemberName.Equals(partyMemberName))
                        {
                            this.AddTeamMember(partyMemberName, partyMember->HomeWorld, partyMember->ClassJob, true);
                            groupManagerIndexLeft.Remove(j);
                            break;
                        }
                    }
                }
            }
        }

        for (var i = 0; i < 20; i++)
        {
            var allianceMember = group.GetAllianceMemberByIndex(i);
            if (allianceMember != null)
            {
                this.AddTeamMember(allianceMember->NameString, allianceMember->HomeWorld, allianceMember->ClassJob, false);
            }
        }
    }

    private void AddTeamMember(string fullName, ushort worldId, uint jobId, bool isInParty)
    {
        var world = Util.GetWorld(worldId);
        if (!Util.IsWorldValid(world))
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
