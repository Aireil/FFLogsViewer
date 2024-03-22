using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FFLogsViewer.Manager;
using FFLogsViewer.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FFLogsViewer;

public class FFLogsClient
{
    public volatile bool IsTokenValid;
    public int LimitPerHour;
    public bool HasLimitPerHourFailed => this.rateLimitDataFetchAttempts >= 3;

    private readonly HttpClient httpClient;
    private readonly object lastCacheRefreshLock = new();
    private readonly ConcurrentDictionary<string, dynamic?> cache = new();
    private volatile bool isRateLimitDataLoading;
    private volatile int rateLimitDataFetchAttempts = 5;
    private DateTime? lastCacheRefresh;

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
        var zoneCount = GetZoneInfo().Count();
        if (zoneCount == 0)
        {
            return 1;
        }

        return zoneCount * 5;
    }

    public void ClearCache()
    {
        this.cache.Clear();
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
                Service.PluginLog.Error($"FF Logs token couldn't be set: {(token == null ? "return was null" : token.Error)}");
            }
        });
    }

    public async Task FetchGameData()
    {
        if (!this.IsTokenValid)
        {
            Service.PluginLog.Error("FFLogs token not set.");
            return;
        }

        const string baseAddress = "https://www.fflogs.com/api/v2/client";
        const string query = """{"query":"{worldData {expansions {name id zones {name id difficulties {name id} encounters {name id}}}}}"}""";

        var content = new StringContent(query, Encoding.UTF8, "application/json");

        try
        {
            var dataResponse = await this.httpClient.PostAsync(baseAddress, content);
            var jsonContent = await dataResponse.Content.ReadAsStringAsync();
            Service.GameDataManager.SetDataFromJson(jsonContent);
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error(ex, "Error while fetching game data.");
        }
    }

    public async Task<dynamic?> FetchLogs(CharData charData)
    {
        if (!this.IsTokenValid)
        {
            Service.PluginLog.Error("FFLogs token not valid.");
            return null;
        }

        Service.HistoryManager.AddHistoryEntry(charData);

        const string baseAddress = "https://www.fflogs.com/api/v2/client";

        var query = BuildQuery(charData);

        try
        {
            var isCaching = Service.Configuration.IsCachingEnabled;
            if (isCaching)
            {
                this.CheckCache();
            }

            dynamic? deserializeJson = null;
            var isCached = isCaching && this.cache.TryGetValue(query, out deserializeJson);

            if (!isCached)
            {
                var content = new StringContent(query, Encoding.UTF8, "application/json");
                var dataResponse = await this.httpClient.PostAsync(baseAddress, content);
                var jsonContent = await dataResponse.Content.ReadAsStringAsync();
                deserializeJson = JsonConvert.DeserializeObject(jsonContent);

                if (isCaching)
                {
                    this.cache.TryAdd(query, deserializeJson);
                }
            }

            return deserializeJson;
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error(ex, "Error while fetching data.");
            return null;
        }
    }

    public void RefreshRateLimitData(bool resetFetchAttempts = false)
    {
        if (resetFetchAttempts)
        {
            this.rateLimitDataFetchAttempts = 0;
        }

        if (this.isRateLimitDataLoading || (this.LimitPerHour <= 0 && this.HasLimitPerHourFailed))
        {
            return;
        }

        this.isRateLimitDataLoading = true;

        Interlocked.Increment(ref this.rateLimitDataFetchAttempts);

        this.LimitPerHour = 0;

        Task.Run(async () =>
        {
            var rateLimitData = await this.FetchRateLimitData().ConfigureAwait(false);

            if (rateLimitData != null && rateLimitData["error"] == null)
            {
                var limitPerHour = rateLimitData["data"]?["rateLimitData"]?["limitPerHour"]?.ToObject<int>();
                if (limitPerHour is null or <= 0)
                {
                    Service.PluginLog.Error($"Couldn't find proper limit per hour: {rateLimitData}");
                }
                else
                {
                    this.LimitPerHour = limitPerHour.Value;
                    this.rateLimitDataFetchAttempts = 0;
                }
            }
            else
            {
                Service.PluginLog.Error($"FF Logs rate limit data couldn't be fetched: {(rateLimitData == null ? "return was null" : rateLimitData["error"])}");
            }

            this.isRateLimitDataLoading = false;
        });
    }

    public void InvalidateCache(CharData charData)
    {
        if (Service.Configuration.IsCachingEnabled)
        {
            var query = BuildQuery(charData);
            this.cache.Remove(query, out _);
        }
    }

    private static string BuildQuery(CharData charData)
    {
        var query = new StringBuilder();
        query.Append(
            $"{{\"query\":\"query {{characterData{{character(name: \\\"{charData.FirstName} {charData.LastName}\\\"serverSlug: \\\"{charData.WorldName}\\\"serverRegion: \\\"{charData.RegionName}\\\"){{");
        query.Append("hidden ");

        var metric = Service.MainWindow.GetCurrentMetric();
        charData.LoadedMetric = metric;
        foreach (var (id, difficulty, isForcingADPS) in GetZoneInfo())
        {
            query.Append($"Zone{id}diff{difficulty}: zoneRankings(zoneID: {id}, difficulty: {difficulty}, metric: ");
            if (isForcingADPS && (Service.MainWindow.OverriddenMetric == null
                                  || Service.MainWindow.OverriddenMetric.InternalName == Service.Configuration.Metric.InternalName))
            {
                query.Append("dps");
            }
            else
            {
                query.Append($"{metric.InternalName}");
            }

            // do not add if standard, avoid issues with alliance raids that do not support any partition
            if (Service.MainWindow.Partition.Id != -1)
            {
                query.Append($", partition: {Service.MainWindow.Partition.Id}");
            }

            if (Service.MainWindow.Job.Name != "All jobs")
            {
                var specName = Service.MainWindow.Job.Name == "Current job"
                                       ? GameDataManager.Jobs.FirstOrDefault(job => job.Id == charData.JobId)?.GetSpecName()
                                       : Service.MainWindow.Job.GetSpecName();

                if (specName != null)
                {
                    query.Append($", specName: \\\"{specName}\\\"");
                }
            }

            query.Append($", timeframe: {(Service.MainWindow.IsTimeframeHistorical() ? "Historical" : "Today")}");

            query.Append(')');
        }

        query.Append("}}}\"}");

        return query.ToString();
    }

    private static async Task<Token?> FetchToken()
    {
        var client = new HttpClient();

        const string baseAddress = "https://www.fflogs.com/oauth/token";
        const string grantType = "client_credentials";

        var form = new Dictionary<string, string>
        {
            { "grant_type", grantType },
            { "client_id", Service.Configuration.ClientId },
            { "client_secret", Service.Configuration.ClientSecret },
        };

        try
        {
            var tokenResponse = await client.PostAsync(baseAddress, new FormUrlEncodedContent(form));
            var jsonContent = await tokenResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Token>(jsonContent);
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error(ex, "Error while fetching token.");
        }

        return null;
    }

    private static IEnumerable<(int ZoneId, int DifficultyId, bool IsForcingADPS)> GetZoneInfo()
    {
        return Service.Configuration.Layout
                .Where(entry => entry.Type == LayoutEntryType.Encounter)
                .GroupBy(entry => new { entry.ZoneId, entry.DifficultyId })
                .Select(group => (group.Key.ZoneId, group.Key.DifficultyId, IsForcingADPS: group.Any(entry => entry.IsForcingADPS)));
    }

    private void CheckCache()
    {
        lock (this.lastCacheRefreshLock)
        {
            if (this.lastCacheRefresh == null)
            {
                this.lastCacheRefresh = DateTime.Now;
                return;
            }

            // clear cache after an hour
            if ((DateTime.Now - this.lastCacheRefresh.Value).TotalHours > 1)
            {
                this.ClearCache();
                this.lastCacheRefresh = DateTime.Now;
            }
        }
    }

    private async Task<JObject?> FetchRateLimitData()
    {
        if (!this.IsTokenValid)
        {
            Service.PluginLog.Error("FFLogs token not valid.");
            return null;
        }

        const string baseAddress = "https://www.fflogs.com/api/v2/client";
        const string query = """{"query":"{rateLimitData {limitPerHour}}"}""";

        var content = new StringContent(query, Encoding.UTF8, "application/json");

        try
        {
            var dataResponse = await this.httpClient.PostAsync(baseAddress, content);
            var jsonContent = await dataResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<JObject>(jsonContent);
        }
        catch (Exception ex)
        {
            Service.PluginLog.Error(ex, "Error while fetching rate limit data.");
        }

        return null;
    }
}
