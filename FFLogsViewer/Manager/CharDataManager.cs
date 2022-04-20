using System;
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
