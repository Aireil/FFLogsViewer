﻿using System;
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
    public bool HasAllianceMembers;

    public unsafe void UpdateTeamList()
    {
        this.TeamList = [];
        this.HasAllianceMembers = false;

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
                this.AddMembersFromCRGroup(cwProxy->CrossRealmGroups[localIndex]);

                for (var i = 0; i < cwProxy->CrossRealmGroups.Length; i++)
                {
                    if (i == localIndex)
                    {
                        continue;
                    }

                    this.AddMembersFromCRGroup(cwProxy->CrossRealmGroups[i], (uint)i);
                }
            }
        }

        // Add self if not in party
        if (this.TeamList.Count == 0 && Service.ClientState.LocalPlayer != null)
        {
            var selfName = Service.ClientState.LocalPlayer.Name.TextValue;
            var selfWorldId = Service.ClientState.LocalPlayer.HomeWorld.RowId;
            var selfJobId = Service.ClientState.LocalPlayer.ClassJob.RowId;
            this.AddTeamMember(selfName, (ushort)selfWorldId, selfJobId);
        }
    }

    public uint? GetNextAllianceIndex(uint? currentAllianceIndex)
    {
        var allianceIndices = this.TeamList
                                  .Select(member => member.AllianceIndex)
                                  .Distinct()
                                  .ToList();

        if (allianceIndices.Count == 0)
        {
            return null;
        }

        var currentIndex = allianceIndices.IndexOf(currentAllianceIndex);
        var nextIndex = (currentIndex + 1) % allianceIndices.Count;

        return allianceIndices[nextIndex];
    }

    private void AddMembersFromCRGroup(CrossRealmGroup crossRealmGroup, uint? groupIndex = null)
    {
        for (var i = 0; i < crossRealmGroup.GroupMemberCount; i++)
        {
            var groupMember = crossRealmGroup.GroupMembers[i];
            this.AddTeamMember(groupMember.NameString, (ushort)groupMember.HomeWorld, groupMember.ClassJobId, groupIndex);

            if (groupIndex != null)
            {
                this.HasAllianceMembers = true;
            }
        }
    }

    private unsafe void AddMembersFromPartyHud(GroupManager.Group group)
    {
        var partyMemberList = AgentModule.Instance()->GetAgentHUD()->PartyMembers.ToArray();
        var groupManagerIndexLeft = Enumerable.Range(0, group.MemberCount).ToList();

        for (var i = 0; i < group.MemberCount; i++)
        {
            var hudPartyMember = partyMemberList.First(member => member.Index == i);
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
                            this.AddTeamMember(partyMemberName, partyMember->HomeWorld, partyMember->ClassJob);
                            groupManagerIndexLeft.Remove(j);
                            break;
                        }
                    }
                }
            }
        }
    }

    private unsafe void AddMembersFromGroupManager(GroupManager.Group group)
    {
        try
        {
            this.AddMembersFromPartyHud(group);
        }
        catch (Exception e)
        {
            Service.PluginLog.Error($"Falling back to group manager order, exception while trying to get party members from HUD: {e.Message}");

            for (var i = 0; i < group.MemberCount; i++)
            {
                var partyMember = group.GetPartyMemberByIndex(i);
                if (partyMember != null)
                {
                    this.AddTeamMember(partyMember->NameString, partyMember->HomeWorld, partyMember->ClassJob);
                }
            }
        }

        for (var groupIndex = 0; groupIndex < 5; groupIndex++)
        {
            for (var memberIndex = 0; memberIndex < 8; memberIndex++)
            {
                var allianceMember = group.GetAllianceMemberByGroupAndIndex(groupIndex, memberIndex);
                if (allianceMember != null)
                {
                    this.AddTeamMember(allianceMember->NameString, allianceMember->HomeWorld, allianceMember->ClassJob, (uint)groupIndex);
                    this.HasAllianceMembers = true;
                }
            }
        }
    }

    private void AddTeamMember(string fullName, ushort worldId, uint jobId, uint? groupIndex = null)
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

        this.TeamList.Add(new TeamMember { FirstName = splitName[0], LastName = splitName[1], World = world.Name.ToString(), JobId = jobId, AllianceIndex = groupIndex });
    }
}
