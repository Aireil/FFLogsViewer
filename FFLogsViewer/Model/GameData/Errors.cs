using Newtonsoft.Json;

namespace FFLogsViewer.Model.GameData;

public class Errors
{
    [JsonProperty("message")]
    public string? Message { get; set; }
}
