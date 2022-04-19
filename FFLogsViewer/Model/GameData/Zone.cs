using System.Collections.Generic;
using Newtonsoft.Json;

namespace FFLogsViewer.Model.GameData;

public class Zone
{
    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("id")]
    public int? Id { get; set; }

    [JsonProperty("difficulties")]
    public List<Difficulty>? Difficulties { get; set; }

    [JsonProperty("encounters")]
    public List<Encounter>? Encounters { get; set; }
}
