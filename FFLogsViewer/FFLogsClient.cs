using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;
using FFLogsViewer.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FFLogsViewer;

public class FFLogsClient
{
    public volatile bool IsTokenValid;
    public int LimitPerHour;

    private readonly HttpClient httpClient;
    private volatile bool isRateLimitDataLoading;
    private volatile int rateLimitDataFetchAttempts;

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

    public static int EstimateCurrentLayoutPoints()
    {
        var zoneCount = GetZoneInfo().Count;
        if (zoneCount == 0)
        {
            return 1;
        }

        return GetZoneInfo().Count * 5;
    }

    public void SetToken()
    {
        this.IsTokenValid = false;
        this.rateLimitDataFetchAttempts = 0;

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
                PluginLog.Error($"FF Logs token couldn't be set: {(token == null ? "return was null" : token.Error)}");
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

        try
        {
            var dataResponse = await this.httpClient.PostAsync(baseAddress, content);
            var jsonContent = await dataResponse.Content.ReadAsStringAsync();
            Service.GameDataManager.SetDataFromJson(jsonContent);
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Error while fetching game data.");
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

        var metric = Service.MainWindow.OverriddenMetric ?? Service.Configuration.Metric;
        charData.LoadedMetric = metric;
        foreach (var (id, difficulty) in GetZoneInfo())
        {
            query.Append($"Zone{id}diff{difficulty}: zoneRankings(zoneID: {id}, difficulty: {difficulty}, metric: {metric.InternalName}, partition: {Service.MainWindow.Partition.Id}");
            if (Service.MainWindow.Job.Name != "All jobs")
            {
                query.Append($", specName: \\\"{Service.MainWindow.Job.Name.Replace(" ", string.Empty)}\\\"");
            }

            query.Append(')');
        }

        query.Append("}}}\"}");

        var content = new StringContent(query.ToString(), Encoding.UTF8, "application/json");

        try
        {
            var dataResponse = await this.httpClient.PostAsync(baseAddress, content);
            var jsonContent = await dataResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject(jsonContent);
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Error while fetching data.");
            return null;
        }
    }

    public void RefreshRateLimitData()
    {
        if (this.isRateLimitDataLoading || (this.LimitPerHour <= 0 && this.rateLimitDataFetchAttempts >= 3))
        {
            return;
        }

        this.isRateLimitDataLoading = true;

        // don't count as an attempt if the previous refresh was successful
        if (this.LimitPerHour <= 0)
        {
            Interlocked.Increment(ref this.rateLimitDataFetchAttempts);
        }

        this.LimitPerHour = 0;

        Task.Run(async () =>
        {
            var rateLimitData = await this.FetchRateLimitData().ConfigureAwait(false);

            if (rateLimitData != null && rateLimitData["error"] == null)
            {
                var limitPerHour = rateLimitData["data"]?["rateLimitData"]?["limitPerHour"]?.ToObject<int>();
                if (limitPerHour is null or <= 0)
                {
                    PluginLog.Error($"Couldn't find proper limit per hour: {rateLimitData}");
                }
                else
                {
                    this.LimitPerHour = limitPerHour.Value;
                }
            }
            else
            {
                PluginLog.Error($"FF Logs rate limit data couldn't be fetched: {(rateLimitData == null ? "return was null" : rateLimitData["error"])}");
            }

            this.isRateLimitDataLoading = false;
        });
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

        try
        {
            var tokenResponse = await client.PostAsync(baseAddress, new FormUrlEncodedContent(form!));
            var jsonContent = await tokenResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Token>(jsonContent);
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Error while fetching token.");
        }

        return null;
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

    private async Task<JObject?> FetchRateLimitData()
    {
        if (!this.IsTokenValid)
        {
            PluginLog.Error("FFLogs token not valid.");
            return null;
        }

        const string baseAddress = @"https://www.fflogs.com/api/v2/client";
        const string query = @"{""query"":""{rateLimitData {limitPerHour}}""}";

        var content = new StringContent(query, Encoding.UTF8, "application/json");

        try
        {
            var dataResponse = await this.httpClient.PostAsync(baseAddress, content);
            var jsonContent = await dataResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<JObject>(jsonContent);
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Error while fetching rate limit data.");
        }

        return null;
    }
}
