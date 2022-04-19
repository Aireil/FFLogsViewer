using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Lumina.Excel.GeneratedSheets;

namespace FFLogsViewer.Manager;

public class CharDataManager
{
    public CharData DisplayedChar = new();
    public string[] ValidWorlds;

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
        var world = Service.DataManager.GetExcelSheet<World>()
                              ?.FirstOrDefault(
                                  x => x.Name.ToString().Equals(worldName, StringComparison.InvariantCultureIgnoreCase));

        if (world == null)
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

                if (placeholder->Name != null && character->HomeWorld != 0 && character->HomeWorld != 65535)
                {
                    var world = Service.DataManager.GetExcelSheet<World>()
                        ?.FirstOrDefault(x => x.RowId == character->HomeWorld);

                    if (world != null)
                    {
                        var name = $"{Util.ReadSeString(placeholder->Name)}@{world.Name}";
                        return name;
                    }
                }
            }
        }
        catch (Exception e)
        {
            PluginLog.Error(e, "Error while resolving placeholder.");
            return null;
        }

        return null;
    }

    public static unsafe List<string> GetPartyMembers()
    {
        List<string> partyMembers = new();
        if (Service.PartyList.Length != 0)
        {
            foreach (var partyMember in Service.PartyList)
            {
                var world = partyMember.World.GameData?.Name;
                if (world == null)
                {
                    continue;
                }

                partyMembers.Add($"{partyMember.Name}@{world}");
            }
        }
        else
        {
            var cwProxy = FFXIVClientStructs.FFXIV.Client.UI.Info.InfoProxyCrossRealm.Instance();
            if (cwProxy->IsInCrossRealmParty != 0)
            {
                foreach (var group in cwProxy->CrossRealmGroupSpan)
                {
                    foreach (var groupMember in group.GroupMemberSpan)
                    {
                        var worldId = groupMember.HomeWorld;
                        var world = Service.DataManager.GetExcelSheet<World>()
                                           ?.FirstOrDefault(x => x.RowId == worldId);
                        if (world == null)
                        {
                            continue;
                        }

                        var name = Util.ReadSeString(groupMember.Name);
                        partyMembers.Add($"{name}@{world.Name}");
                    }
                }
            }
        }

        var selfName = Service.ClientState.LocalPlayer?.Name;
        var selfWorldId = Service.ClientState.LocalPlayer?.HomeWorld.Id;
        var selfWorld = Service.DataManager.GetExcelSheet<World>()
                               ?.FirstOrDefault(x => x.RowId == selfWorldId);
        if (selfName != null && selfWorld != null)
        {
            partyMembers.Remove($"{selfName}@{selfWorld.Name}");
        }

        return partyMembers;
    }

    public static void OpenCharInBrowser(string name)
    {
        var charData = new CharData();
        if (charData.ParseTextForChar(name))
        {
            Util.OpenLink(charData);
        }
    }

    public void ResetDisplayedChar()
    {
        this.DisplayedChar = new CharData();
    }
}
