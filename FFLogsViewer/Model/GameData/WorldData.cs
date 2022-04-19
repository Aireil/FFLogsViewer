using System.Collections.Generic;
using Newtonsoft.Json;

namespace FFLogsViewer.Model.GameData;

public class WorldData
{
    [JsonProperty("expansions")]
    public List<Expansion>? Expansions { get; set; }
}
