using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Lumina.Excel.GeneratedSheets;

namespace FFLogsViewer.Manager;

public class PartyListManager
{
    public List<(string Name, string World)> PartyList = new();

    public void Update()
    {
        this.PartyList = GetPartyMembers();
    }

    private static unsafe List<(string Name, string World)> GetPartyMembers()
    {
        List<(string, string)> partyMembers = new();
        if (Service.PartyList.Length != 0)
        {
            foreach (var partyMember in Service.PartyList)
            {
                var world = partyMember.World.GameData?.Name;
                if (world == null)
                {
                    continue;
                }

                partyMembers.Add((partyMember.Name.ToString(), world));
            }
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
            if (selfName != null && selfWorld != null)
            {
                partyMembers.Add((selfName.ToString(), selfWorld.Name.ToString()));
            }
        }

        return partyMembers;
    }

    private static unsafe void AddMembersFromCRGroup(ICollection<(string Name, string World)> partyMembers, CrossRealmGroup crossRealmGroup)
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
            partyMembers.Add((name.ToString(), world.Name.ToString()));
        }
    }
}
