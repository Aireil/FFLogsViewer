using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Plugin;
using Newtonsoft.Json;

namespace FFLogsViewer
{
    public class FfLogsClient
    {
        public static async Task<Token> GetToken(string clientId, string clientSecret)
        {
            var client = new HttpClient();
            const string baseAddress = @"https://www.fflogs.com/oauth/token";

            const string grantType = "client_credentials";

            var form = new Dictionary<string, string>
            {
                {"grant_type", grantType},
                {"client_id", clientId},
                {"client_secret", clientSecret},
            };

            var tokenResponse = await client.PostAsync(baseAddress, new FormUrlEncodedContent(form));
            var jsonContent = await tokenResponse.Content.ReadAsStringAsync();
            var tok = JsonConvert.DeserializeObject<Token>(jsonContent);
            return tok;
        }

        public static async Task<dynamic> GetLogsData(CharacterData characterData, Token token)
        {
            var client = new HttpClient();
            const string baseAddress = @"https://www.fflogs.com/api/v2/client";

            client.DefaultRequestHeaders.Authorization
                = new AuthenticationHeaderValue("Bearer", token.AccessToken);

            var query =
                $"{{\"query\":\"query {{characterData{{character(name: \\\"{characterData.FirstName} {characterData.LastName}\\\"serverSlug: \\\"{characterData.WorldName}\\\"serverRegion: \\\"{characterData.RegionName}\\\"){{"
                + "hidden "
                + "EdenPromise: zoneRankings(zoneID: 38, , difficulty: 101)"
                + "EdenVerse: zoneRankings(zoneID: 33, , difficulty: 101)"
                + "ExtremesII: zoneRankings(zoneID: 34)"
                + "ExtremesIII: zoneRankings(zoneID: 37)"
                + "Unreal: zoneRankings(zoneID: 36)"
                + "UltimatesShB: zoneRankings(zoneID: 32)"
                + "UltimatesSB: zoneRankings(zoneID: 30)"
                + "}}}\"}";

            var content = new StringContent(query, Encoding.UTF8, "application/json");

            var dataResponse = await client.PostAsync(baseAddress, content);
            try
            {
                var jsonContent = await dataResponse.Content.ReadAsStringAsync();
                dynamic json = JsonConvert.DeserializeObject(jsonContent);
                return json;
            }
            catch (Exception e)
            {
                PluginLog.LogError(e.Message);
                PluginLog.LogError(e.StackTrace);
                return null;
            }
        }

        public class Token
        {
            [JsonProperty("access_token")] public string AccessToken { get; set; }

            [JsonProperty("token_type")] public string TokenType { get; set; }

            [JsonProperty("expires_in")] public int ExpiresIn { get; set; }
        }
    }
}