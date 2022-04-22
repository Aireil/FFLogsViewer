using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Logging;
using FFLogsViewer.Model;
using Newtonsoft.Json;

namespace FFLogsViewer;

public class FFLogsClient
{
    public bool IsTokenValid;

    private readonly HttpClient httpClient;

    public class Token
    {
        [JsonProperty("access_token")]
        public string? AccessToken { get; set; }

        [JsonProperty("token_type")]
        public string? TokenType { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("error")]
        public string? Error { get; set; }
    }

    public FFLogsClient()
    {
        this.httpClient = new HttpClient();

        this.SetToken();
    }

    public static bool IsConfigSet()
    {
        return !string.IsNullOrEmpty(Service.Configuration.ClientId)
               && !string.IsNullOrEmpty(Service.Configuration.ClientSecret);
    }

    public void SetToken()
    {
        this.IsTokenValid = false;

        if (!IsConfigSet())
        {
            return;
        }

        Task.Run(async () =>
        {
            var token = await FetchToken().ConfigureAwait(false);

            if (token is { Error: null })
            {
                this.httpClient.DefaultRequestHeaders.Authorization
                    = new AuthenticationHeaderValue("Bearer", token.AccessToken);

                this.IsTokenValid = true;
            }
            else
            {
                PluginLog.Error("FF Logs token couldn't be set.");
            }
        });
    }

    public async Task FetchGameData()
    {
        if (!this.IsTokenValid)
        {
            PluginLog.Error("FFLogs token not set.");
            return;
        }

        const string baseAddress = @"https://www.fflogs.com/api/v2/client";
        const string query = @"{""query"":""{worldData {expansions {name id zones {name id difficulties {name id} encounters {name id}}}}}""}";

        var content = new StringContent(query, Encoding.UTF8, "application/json");

        var dataResponse = await this.httpClient.PostAsync(baseAddress, content);
        try
        {
            var jsonContent = await dataResponse.Content.ReadAsStringAsync();
            Service.GameDataManager.SetDataFromJson(jsonContent);
        }
        catch (Exception e)
        {
            PluginLog.Error(e, "Error while fetching game data.");
        }
    }

    public async Task<dynamic?> FetchLogs(CharData charData)
    {
        if (!this.IsTokenValid)
        {
            PluginLog.Error("FFLogs token not valid.");
            return null;
        }

        const string baseAddress = @"https://www.fflogs.com/api/v2/client";

        var query = new StringBuilder();
        query.Append(
            $"{{\"query\":\"query {{characterData{{character(name: \\\"{charData.FirstName} {charData.LastName}\\\"serverSlug: \\\"{charData.WorldName}\\\"serverRegion: \\\"{charData.RegionName}\\\"){{");
        query.Append("hidden ");

        var metric = charData.OverriddenMetric ?? Service.Configuration.Metric;
        charData.LoadedMetric = metric;
        foreach (var (id, difficulty) in GetZoneInfo())
        {
            query.Append($"Zone{id}diff{difficulty}: zoneRankings(zoneID: {id}, difficulty: {difficulty}, metric: {metric.InternalName}");
            if (charData.Job.Name != "All jobs")
            {
                query.Append($", specName: \\\"{charData.Job.Name.Replace(" ", string.Empty)}\\\"");
            }

            query.Append(')');
        }

        query.Append("}}}\"}");

        var content = new StringContent(query.ToString(), Encoding.UTF8, "application/json");

        var dataResponse = await this.httpClient.PostAsync(baseAddress, content);
        try
        {
            var jsonContent = await dataResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject(jsonContent);
        }
        catch (Exception e)
        {
            PluginLog.Error(e, "Error while fetching data.");
            return null;
        }
    }

    private static async Task<Token?> FetchToken()
    {
        var client = new HttpClient();

        const string baseAddress = @"https://www.fflogs.com/oauth/token";
        const string grantType = "client_credentials";

        var form = new Dictionary<string, string>
        {
            { "grant_type", grantType },
            { "client_id", Service.Configuration.ClientId ?? string.Empty },
            { "client_secret", Service.Configuration.ClientSecret ?? string.Empty },
        };

        var tokenResponse = await client.PostAsync(baseAddress, new FormUrlEncodedContent(form!));
        var jsonContent = await tokenResponse.Content.ReadAsStringAsync();
        var tok = JsonConvert.DeserializeObject<Token>(jsonContent);
        return tok;
    }

    private static List<Tuple<int, int>> GetZoneInfo()
    {
        var info = new List<Tuple<int, int>>();
        foreach (var entry in Service.Configuration.Layout)
        {
            if (entry.Type == LayoutEntryType.Encounter)
            {
                var isInInfo = false;
                foreach (var (id, difficulty) in info)
                {
                    if (id == entry.ZoneId && difficulty == entry.DifficultyId)
                    {
                        isInInfo = true;
                        break;
                    }
                }

                if (!isInInfo)
                {
                    info.Add(new Tuple<int, int>(entry.ZoneId, entry.DifficultyId));
                }
            }
        }

        return info;
    }
}
