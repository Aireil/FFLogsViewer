using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Command;
using Dalamud.Logging;
using Dalamud.Plugin;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using XivCommon;

namespace FFLogsViewer
{
    public class FFLogsViewer : IDalamudPlugin
    {
        private const string CommandName = "/fflogs";
        private const string SettingsCommandName = "/fflogsconfig";

        private readonly string[] _eu =
        {
            "Cerberus", "Louisoix", "Moogle", "Omega", "Ragnarok", "Spriggan",
            "Lich", "Odin", "Phoenix", "Shiva", "Zodiark", "Twintania",
        };

        private readonly string[] _jp =
        {
            "Aegis", "Atomos", "Carbuncle", "Garuda", "Gungnir", "Kujata", "Ramuh", "Tonberry", "Typhon", "Unicorn",
            "Alexander", "Bahamut", "Durandal", "Fenrir", "Ifrit", "Ridill", "Tiamat", "Ultima", "Valefor", "Yojimbo",
            "Zeromus",
            "Anima", "Asura", "Belias", "Chocobo", "Hades", "Ixion", "Mandragora", "Masamune", "Pandaemonium",
            "Shinryu", "Titan",
        };

        private readonly string[] _na =
        {
            "Adamantoise", "Cactuar", "Faerie", "Gilgamesh", "Jenova", "Midgardsormr", "Sargatanas", "Siren",
            "Behemoth", "Excalibur", "Exodus", "Famfrit", "Hyperion", "Lamia", "Leviathan", "Ultros",
            "Balmung", "Brynhildr", "Coeurl", "Diabolos", "Goblin", "Malboro", "Mateus", "Zalera",
        };

        internal readonly Configuration Configuration;
        internal readonly FfLogsClient FfLogsClient;
        internal readonly PluginUi Ui;

        private readonly DalamudPluginInterface _pi;
        private readonly CommandManager _commandManager;
        internal XivCommonBase Common { get; private set; }
        private ContextMenu ContextMenu { get; set; }

        public string Name => "FF Logs Viewer";

        public FFLogsViewer(DalamudPluginInterface pluginInterface, CommandManager commandManager)
        {
            this._pi = pluginInterface;
            this._pi.Create<DalamudApi>();
            this._commandManager = commandManager;

            this.Configuration = this._pi.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this._pi);

            this.FfLogsClient = new FfLogsClient(this);

            this.Ui = new PluginUi(this);

            if (this.Configuration.ContextMenu)
            {
                this.Common = new XivCommonBase(Hooks.ContextMenu);
                this.ContextMenu = new ContextMenu(this);
            }

            this._commandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Open the FF Logs Viewer window or parse the arguments for a character.",
                ShowInHelp = true,
            });

            this._commandManager.AddHandler(SettingsCommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Open the FF Logs Viewer config window.",
                ShowInHelp = true,
            });

            this._pi.UiBuilder.Draw += DrawUi;
            this._pi.UiBuilder.OpenConfigUi += ToggleSettingsUi;
        }

        public void Dispose()
        {
            this.Common?.Dispose();
            this.ContextMenu?.Dispose();
            this.Ui.Dispose();
            this._commandManager.RemoveHandler(CommandName);
            this._commandManager.RemoveHandler(SettingsCommandName);
        }

        private void OnCommand(string command, string args)
        {
            switch (command)
            {
                case SettingsCommandName:
                    this.Ui.SettingsVisible = !this.Ui.SettingsVisible;
                    break;
                case CommandName when string.IsNullOrEmpty(args):
                    this.Ui.Visible = !this.Ui.Visible;
                    break;
                case CommandName when args.Equals("config", StringComparison.OrdinalIgnoreCase):
                    this.Ui.SettingsVisible = !this.Ui.SettingsVisible;
                    break;
                case CommandName:
                    SearchPlayer(args);
                    break;
            }
        }

        private void DrawUi()
        {
            this.Ui.Draw();
        }

        public bool IsConfigSetup()
        {
            return this.Configuration.ClientId != null && this.Configuration.ClientSecret != null;
        }

        private void ToggleSettingsUi()
        {
            this.Ui.SettingsVisible = !this.Ui.SettingsVisible;
        }

        internal void ToggleContextMenuButton(bool enable)
        {
            switch (enable)
            {
                case true when this.ContextMenu != null:
                case false when this.ContextMenu == null:
                    return;
                case true:
                    this.Common = new XivCommonBase(Hooks.ContextMenu);
                    this.ContextMenu = new ContextMenu(this);
                    break;
                default:
                    this.Common?.Dispose();
                    this.ContextMenu?.Dispose();
                    this.ContextMenu = null;
                    this.Common = null;
                    break;
            }
        }

        internal void SearchPlayer(string args)
        {
            try
            {
                this.Ui.Visible = true;
                this.Ui.SetCharacterAndFetchLogs(ParseTextForChar(args));
            }
            catch
            {
                this.Ui.SetErrorMessage("Character could not be found.");
            }
        }

        internal void OpenPlayerInBrowser(string args)
        {
            try
            {
                var character = ParseTextForChar(args);
                Process.Start(new ProcessStartInfo()
                {
                    FileName = $"https://www.fflogs.com/character/{GetRegionName(character.WorldName)}/{character.WorldName}/{character.FirstName} {character.LastName}",
                    UseShellExecute = true,
                });

            }
            catch
            {
                this.Ui.SetErrorMessage("Character could not be found.");
            }
        }

        private static CharacterData GetPlayerData(PlayerCharacter playerCharacter)
        {
            return new()
            {
                FirstName = playerCharacter.Name.TextValue.Split(' ')[0],
                LastName = playerCharacter.Name.TextValue.Split(' ')[1],
                WorldName = playerCharacter.HomeWorld.GameData.Name,
            };
        }

        internal CharacterData GetTargetCharacter()
        {
            var target = DalamudApi.TargetManager.Target;
            if (target is PlayerCharacter targetCharacter && target.ObjectKind != ObjectKind.Companion)
                return GetPlayerData(targetCharacter);

            throw new ArgumentException("Not a valid target.");
        }

        private static bool IsWorldValid(string worldAttempt)
        {
            var world = DalamudApi.DataManager.GetExcelSheet<World>()
                ?.FirstOrDefault(
                    x => x.Name.ToString().Equals(worldAttempt, StringComparison.InvariantCultureIgnoreCase));

            return world != null;
        }

        internal CharacterData GetClipboardCharacter()
        {
            if (ImGui.GetClipboardText() == null) throw new ArgumentException("Invalid clipboard.");

            var clipboardRawText = ImGui.GetClipboardText();
            return ParseTextForChar(clipboardRawText);
        }

        private CharacterData ParseTextForChar(string rawText)
        {
            var character = new CharacterData();
            rawText = rawText.Replace("'s party for", " ");

            rawText = rawText.Replace("You join", " ");
            rawText = Regex.Replace(rawText, "\\[.*?\\]", " ");
            rawText = Regex.Replace(rawText, "[^A-Za-z '-]", " ");
            rawText = string.Concat(rawText.Select(x => char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' ');
            rawText = Regex.Replace(rawText, @"\s+", " ");

            var words = rawText.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);

            var index = -1;
            for (var i = 0; index == -1 && i < this._na.Length; i++) index = Array.IndexOf(words, this._na[i]);
            for (var i = 0; index == -1 && i < this._eu.Length; i++) index = Array.IndexOf(words, this._eu[i]);
            for (var i = 0; index == -1 && i < this._jp.Length; i++) index = Array.IndexOf(words, this._jp[i]);

            if (index - 2 >= 0)
            {
                character.FirstName = words[index - 2];
                character.LastName = words[index - 1];
                character.WorldName = words[index];
            }
            else if (words.Length >= 2)
            {
                character.FirstName = words[0];
                character.LastName = words[1];
                character.WorldName = DalamudApi.ClientState.LocalPlayer?.HomeWorld.GameData.Name;
            }
            else
            {
                throw new ArgumentException("Invalid text.");
            }

            if (!char.IsUpper(character.FirstName[0]) || !char.IsUpper(character.LastName[0]))
                throw new ArgumentException("Invalid text.");

            return character;
        }

        internal void FetchLogs(CharacterData characterData)
        {
            if (characterData.IsEveryLogsReady) characterData.ResetLogs();

            try
            {
                characterData.RegionName = GetRegionName(characterData.WorldName);
            }
            catch (Exception e)
            {
                this.Ui.SetErrorMessage("World not supported or invalid.");
                PluginLog.LogError(e, "World not supported or invalid.");
                return;
            }


            characterData.IsDataLoading = true;
            Task.Run(async () =>
            {
                var logData = await this.FfLogsClient.GetLogs(characterData).ConfigureAwait(false);
                if (logData?.data?.characterData?.character == null)
                {
                    if (logData?.errors != null)
                    {
                        characterData.IsDataLoading = false;
                        this.Ui.SetErrorMessage("Malformed GraphQL query.");
                        PluginLog.Log($"Malformed GraphQL query: {logData}");
                        return;
                    }

                    if (logData?.error != null && logData.error == "Unauthenticated.")
                    {
                        characterData.IsDataLoading = false;
                        this.Ui.SetErrorMessage("API Client not valid, check config.");
                        PluginLog.Log($"Unauthenticated: {logData}");
                        return;
                    }

                    if (logData == null)
                    {
                        characterData.IsDataLoading = false;
                        this.Ui.SetErrorMessage("Could not reach FF Logs servers.");
                        PluginLog.Log("Could not reach FF Logs servers.");
                        return;
                    }

                    characterData.IsDataLoading = false;
                    this.Ui.SetErrorMessage("Character not found.");
                    return;
                }

                try
                {
                    if (logData.data.characterData.character.hidden == "true")
                    {
                        characterData.IsDataLoading = false;
                        this.Ui.SetErrorMessage(
                            $"{characterData.FirstName} {characterData.LastName}@{characterData.WorldName}'s logs are hidden.");
                        return;
                    }

                    ParseLogs(characterData, logData.data.characterData.character.EdenPromise);
                    //ParseLogs(characterData, logData.data.characterData.character.EdenVerse);
                    ParseLogs(characterData, logData.data.characterData.character.Asphodelos);
                    ParseLogs(characterData, logData.data.characterData.character.UltimatesShB);
                    ParseLogs(characterData, logData.data.characterData.character.UltimatesSB);
                    //ParseLogs(characterData, logData.data.characterData.character.ExtremesII);
                    //ParseLogs(characterData, logData.data.characterData.character.ExtremesIII);
                    ParseLogs(characterData, logData.data.characterData.character.ExtremesEW);
                    //ParseLogs(characterData, logData.data.characterData.character.Unreal);
                }
                catch (Exception e)
                {
                    characterData.IsDataLoading = false;
                    this.Ui.SetErrorMessage("Could not load data from FF Logs servers.");
                    PluginLog.LogError(e, "Could not load data from FF Logs servers.");
                    return;
                }

                characterData.IsEveryLogsReady = true;
                characterData.LoadedFirstName = characterData.FirstName;
                characterData.LoadedLastName = characterData.LastName;
                characterData.LoadedWorldName = characterData.WorldName;
                characterData.IsDataLoading = false;
            }).ContinueWith(t =>
            {
                if (!t.IsFaulted) return;
                characterData.IsDataLoading = false;
                this.Ui.SetErrorMessage("Networking error, please try again.");
                if (t.Exception == null) return;
                foreach (var e in t.Exception.Flatten().InnerExceptions)
                {
                    PluginLog.LogError(e, "Networking error.");
                }
            });
        }

        private string GetRegionName(string worldName)
        {
            if (!IsWorldValid(worldName)) throw new ArgumentException("Invalid world.");

            if (this._na.Any(worldName.Contains)) return "NA";

            if (this._eu.Any(worldName.Contains)) return "EU";

            if (this._jp.Any(worldName.Contains)) return "JP";

            throw new ArgumentException("World not supported.");
        }

        private static void ParseLogs(CharacterData characterData, dynamic zone)
        {
            if (zone?.rankings == null || zone.rankings.Count == 0)
                throw new InvalidOperationException("Field zone.rankings not found or empty.");
            foreach (var fight in zone.rankings)
            {
                if (fight?.encounter == null) throw new InvalidOperationException("Field fight.encounter not found.");

                int bossId = fight.encounter.id;
                int best;
                int median;
                int kills;
                string job;
                if (fight.spec == null)
                {
                    best = -1;
                    median = -1;
                    kills = -1;
                    job = "-";
                }
                else
                {
                    best = Convert.ToInt32(Math.Floor((double) fight.rankPercent));
                    median = Convert.ToInt32(Math.Floor((double) fight.medianPercent));
                    kills = Convert.ToInt32(Math.Floor((double) fight.totalKills));
                    job = Regex.Replace(fight.spec.ToString(), "([a-z])([A-Z])", "$1 $2");
                }

                characterData.Bests.Add(bossId, best);
                characterData.Medians.Add(bossId, median);
                characterData.Kills.Add(bossId, kills);
                characterData.Jobs.Add(bossId, job);
            }
        }

        private void FetchLogsData()
        {
            Task.Run(async () =>
            {
                var logData = await this.FfLogsClient.GetData().ConfigureAwait(false);
                try
                {
                    foreach (var expansion in logData.Data.WorldData.Expansions)
                    {
                        PluginLog.Information(expansion.Name);
                        PluginLog.Information(expansion.Id.ToString());
                        foreach (var zone in expansion.Zones)
                        {
                            PluginLog.Information(zone.Name);
                            PluginLog.Information(zone.Id.ToString());
                            foreach (var difficulty in zone.Difficulties)
                            {
                                PluginLog.Information(difficulty.Name);
                                PluginLog.Information(difficulty.Id.ToString());
                            }

                            foreach (var encounter in zone.Encounters)
                            {
                                PluginLog.Information(encounter.Name);
                                PluginLog.Information(encounter.Id.ToString());
                            }

                        }
                    }
                }
                catch (Exception e)
                {
                    PluginLog.LogError(e, "Could not load data from FF Logs servers.");
                }

            }).ContinueWith(t =>
            {
                if (!t.IsFaulted) return;
                if (t.Exception == null) return;
                foreach (var e in t.Exception.Flatten().InnerExceptions)
                {
                    PluginLog.LogError(e, "Networking error.");
                }
            });
        }
    }
}
