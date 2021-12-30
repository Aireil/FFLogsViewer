using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Logging;
using Dalamud.Plugin;
using Newtonsoft.Json;

namespace FFLogsViewer
{
    internal class FfLogsClient
    {
        private readonly FFLogsViewer _plugin;
        private Token _token;
        private readonly HttpClient _httpClient;
        internal bool IsTokenValid = false;

        internal class Token
        {
            [JsonProperty("access_token")] internal string AccessToken { get; set; }

            [JsonProperty("token_type")] internal string TokenType { get; set; }

            [JsonProperty("expires_in")] internal int ExpiresIn { get; set; }

            [JsonProperty("error")] internal string Error { get; set; }
        }

        internal FfLogsClient(FFLogsViewer plugin)
        {
            this._plugin = plugin;
            this._httpClient = new HttpClient();

            SetToken();
        }

        internal void SetToken()
        {
            if (this._plugin.Configuration.ClientId == null ||
                this._plugin.Configuration.ClientSecret == null) return;

            this.IsTokenValid = false;
            this._token = null;

            Task.Run(async () =>
            {
                this._token = await GetToken()
                    .ConfigureAwait(false);

                if (this._token is {Error: null})
                {
                    this._httpClient.DefaultRequestHeaders.Authorization
                        = new AuthenticationHeaderValue("Bearer", this._token.AccessToken);

                    this.IsTokenValid = true;
                }
            });
        }

        private async Task<Token> GetToken()
        {
            var client = new HttpClient();
            const string baseAddress = @"https://www.fflogs.com/oauth/token";

            const string grantType = "client_credentials";

            var form = new Dictionary<string, string>
            {
                {"grant_type", grantType},
                {"client_id", this._plugin.Configuration.ClientId},
                {"client_secret", this._plugin.Configuration.ClientSecret},
            };

            var tokenResponse = await client.PostAsync(baseAddress, new FormUrlEncodedContent(form));
            var jsonContent = await tokenResponse.Content.ReadAsStringAsync();
            var tok = JsonConvert.DeserializeObject<Token>(jsonContent);
            return tok;
        }

        internal async Task<dynamic> GetLogs(CharacterData characterData)
        {
            if (this._token == null) return null;

            const string baseAddress = @"https://www.fflogs.com/api/v2/client";

            var query =
                $"{{\"query\":\"query {{characterData{{character(name: \\\"{characterData.FirstName} {characterData.LastName}\\\"serverSlug: \\\"{characterData.WorldName}\\\"serverRegion: \\\"{characterData.RegionName}\\\"){{" +
                "hidden " +
                "EdenPromise: zoneRankings(zoneID: 38, , difficulty: 101)" +
                "EdenVerse: zoneRankings(zoneID: 33, , difficulty: 101)" +
                "Asphodelos: zoneRankings(zoneID: 44, difficulty: 101)" +
                "ExtremesII: zoneRankings(zoneID: 34)" +
                "ExtremesIII: zoneRankings(zoneID: 37)" +
                "ExtremesEW: zoneRankings(zoneID: 42)" +
                "Unreal: zoneRankings(zoneID: 36)" +
                "UltimatesShB: zoneRankings(zoneID: 32)" +
                "UltimatesSB: zoneRankings(zoneID: 30)" +
                "}}}\"}";

            var content = new StringContent(query, Encoding.UTF8, "application/json");

            var dataResponse = await this._httpClient.PostAsync(baseAddress, content);
            try
            {
                var jsonContent = await dataResponse.Content.ReadAsStringAsync();
                dynamic json = JsonConvert.DeserializeObject(jsonContent);
                return json;
            }
            catch (Exception e)
            {
                PluginLog.LogError(e, "Error while fetching data.");
                return null;
            }
        }

        internal async Task<LogsData> GetData()
        {
            if (this._token == null) return null;

            const string baseAddress = @"https://www.fflogs.com/api/v2/client";

            const string query = @"{""query"":""{worldData {expansions {name id zones {name id difficulties {name id} encounters {name id}}}}}""}";

            var content = new StringContent(query, Encoding.UTF8, "application/json");

            var dataResponse = await this._httpClient.PostAsync(baseAddress, content);
            try
            {
                var jsonContent = await dataResponse.Content.ReadAsStringAsync();
                return LogsData.FromJson(jsonContent);
            }
            catch (Exception e)
            {
                PluginLog.LogError(e, "Error while fetching data.");
                return null;
            }
        }
    }
}
