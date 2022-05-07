using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Lumina.Excel.GeneratedSheets;
using PartyMember = FFLogsViewer.Model.PartyMember;

namespace FFLogsViewer.Manager;

public class PartyListManager
{
    public List<PartyMember> PartyList = new();

    public void UpdatePartyList()
    {
        this.PartyList = GetPartyMembers();
    }

    private static unsafe List<PartyMember> GetPartyMembers()
    {
        var partyMembers = new List<PartyMember>();

        var groupManager = GroupManager.Instance();
        if (groupManager->MemberCount > 0)
        {
            AddMembersFromGroupManager(partyMembers, groupManager);
        }
        else
        {
            var cwProxy = InfoProxyCrossRealm.Instance();
            if (cwProxy->IsInCrossRealmParty != 0)
            {
                var localIndex = cwProxy->LocalPlayerGroupIndex;
                AddMembersFromCRGroup(partyMembers, cwProxy->CrossRealmGroupSpan[localIndex]);

                for (var i = 0; i < cwProxy->CrossRealmGroupSpan.Length; i++)
                {
                    if (i == localIndex)
                    {
                        continue;
                    }

                    AddMembersFromCRGroup(partyMembers, cwProxy->CrossRealmGroupSpan[i]);
                }
            }
        }

        if (partyMembers.Count == 0)
        {
            var selfName = Service.ClientState.LocalPlayer?.Name;
            var selfWorldId = Service.ClientState.LocalPlayer?.HomeWorld.Id;
            var selfWorld = Service.DataManager.GetExcelSheet<World>()
                                   ?.FirstOrDefault(x => x.RowId == selfWorldId);
            var selfJobId = Service.ClientState.LocalPlayer?.ClassJob.Id;
            if (selfName != null && selfWorld != null && selfJobId != null)
            {
                partyMembers.Add(new PartyMember { Name = selfName.ToString(), World = selfWorld.Name, JobId = selfJobId.Value });
            }
        }

        return partyMembers;
    }

    private static unsafe void AddMembersFromCRGroup(ICollection<PartyMember> partyMembers, CrossRealmGroup crossRealmGroup)
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
            partyMembers.Add(new PartyMember { Name = name.ToString(), World = world.Name, JobId = groupMember.ClassJobId });
        }
    }

    private static unsafe void AddMembersFromGroupManager(ICollection<PartyMember> partyMembers, GroupManager* groupManager)
    {
        for (var i = 0; i < groupManager->MemberCount; i++)
        {
            var groupMember = groupManager->GetPartyMemberByIndex(i);

            var partyMember = GetPartyMember(groupMember);
            if (partyMember != null && partyMember.Name != string.Empty && partyMember.World != string.Empty)
            {
                partyMembers.Add(partyMember);
            }
        }

        for (var i = 0; i < 20; i++)
        {
            var allianceMember = groupManager->GetAllianceMemberByIndex(i);

            var partyMember = GetPartyMember(allianceMember);
            if (partyMember != null && partyMember.Name != string.Empty && partyMember.World != string.Empty)
            {
                partyMembers.Add(partyMember);
            }
        }
    }

    private static unsafe PartyMember? GetPartyMember(
        FFXIVClientStructs.FFXIV.Client.Game.Group.PartyMember* partyMember)
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
        return new PartyMember { Name = name.ToString(), World = world.Name, JobId = partyMember->ClassJob };
    }
}
