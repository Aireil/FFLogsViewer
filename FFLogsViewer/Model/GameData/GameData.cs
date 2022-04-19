using System.Linq;
using Dalamud.Logging;
using Newtonsoft.Json;

namespace FFLogsViewer.Model.GameData;

public class GameData
{
    [JsonProperty("data")]
    public Data? Data { get; set; }

    [JsonProperty("errors")]
    public Errors? Errors { get; set; }

    public static GameData? FromJson(string json) => JsonConvert.DeserializeObject<GameData>(json);

    public bool IsDataValid()
    {
        var isValid = true;

        if (this.Data?.WorldData?.Expansions == null)
        {
            isValid = false;
        }
        else
        {
            foreach (var expansion in this.Data.WorldData.Expansions)
            {
                if (expansion.Name == null || expansion.Id == null || expansion.Zones == null)
                {
                    isValid = false;
                    break;
                }

                foreach (var zone in expansion.Zones)
                {
                    if (zone.Name == null || zone.Id == null || zone.Difficulties == null || zone.Encounters == null)
                    {
                        isValid = false;
                        break;
                    }

                    if (zone.Difficulties.Any(difficulty => difficulty.Name == null || difficulty.Id == null) ||
                        zone.Encounters.Any(encounter => encounter.Name == null || encounter.Id == null))
                    {
                        isValid = false;
                        break;
                    }
                }
            }
        }

        if (isValid == false)
        {
            PluginLog.Error("Data invalid: " + this.ToJson());
        }

        return isValid;
    }

    public string ToJson() => JsonConvert.SerializeObject(this);
}
