using Newtonsoft.Json;

namespace FFLogsViewer.Model.GameData;

public class Data
{
    [JsonProperty("worldData")]
    public WorldData? WorldData { get; set; }
}
