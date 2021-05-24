using System.Collections.Generic;
using Newtonsoft.Json;

namespace FFLogsViewer
{
    public partial class LogsData
    {
        [JsonProperty("data")]
        public Data Data { get; set; }
    }

    public partial class Data
    {
        [JsonProperty("worldData")]
        public WorldData WorldData { get; set; }
    }

    public partial class WorldData
    {
        [JsonProperty("expansions")]
        public List<Expansion> Expansions { get; set; }
    }

    public partial class Expansion
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("zones")]
        public List<Zone> Zones { get; set; }
    }

    public partial class Zone
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("difficulties")]
        public List<Difficulty> Difficulties { get; set; }

        [JsonProperty("encounters")]
        public List<Encounter> Encounters { get; set; }
    }

    public partial class Difficulty
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }
    }

    public partial class Encounter
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }
    }

    public partial class LogsData
    {
        public static LogsData FromJson(string json) => JsonConvert.DeserializeObject<LogsData>(json, FFLogsViewer.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this LogsData self) => JsonConvert.SerializeObject(self, FFLogsViewer.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        };
    }
}
