using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using XivCommon;

namespace FFLogsViewer
{
    public class FFLogsViewer : IDalamudPlugin
    {
        private const string CommandName = "/fflogs";
        private const string SettingsCommandName = "/fflogsconfig";

        private readonly string[] _validWorlds;

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
                HelpMessage = "Open the FF Logs Viewer window or parse the arguments for a character, support most placeholders.",
                ShowInHelp = true,
            });

            this._commandManager.AddHandler(SettingsCommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Open the FF Logs Viewer config window.",
                ShowInHelp = true,
            });

            var worlds = DalamudApi.DataManager.GetExcelSheet<World>()?.Where(world => world.IsPublic && world.DataCenter?.Value?.Region != 0);

            if (worlds == null)
            {
                throw new InvalidOperationException("Sheets weren't ready.");
            }

            this._validWorlds = worlds.Select(world => world.Name.RawString).ToArray();

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
            return new CharacterData
            {
                FirstName = playerCharacter.Name.TextValue.Split(' ')[0],
                LastName = playerCharacter.Name.TextValue.Split(' ')[1],
                WorldName = playerCharacter.HomeWorld.GameData?.Name,
            };
        }

        internal CharacterData GetTargetCharacter()
        {
            var target = DalamudApi.TargetManager.Target;
            if (target is PlayerCharacter targetCharacter && target.ObjectKind != ObjectKind.Companion)
                return GetPlayerData(targetCharacter);

            throw new ArgumentException("Not a valid target.");
        }

        internal CharacterData GetClipboardCharacter()
        {
            if (ImGui.GetClipboardText() == null) throw new ArgumentException("Invalid clipboard.");

            var clipboardRawText = ImGui.GetClipboardText();
            return ParseTextForChar(clipboardRawText);
        }

        private static unsafe SeString ReadSeString(byte* ptr) {
            var offset = 0;
            while (true) {
                var b = *(ptr + offset);
                if (b == 0) {
                    break;
                }
                offset += 1;
            }
            var bytes = new byte[offset];
            Marshal.Copy(new IntPtr(ptr), bytes, 0, offset);
            return SeString.Parse(bytes);
        }

        private static unsafe string? FindPlaceholder(string text)
        {
            var placeholder = Framework.Instance()->GetUiModule()->GetPronounModule()->ResolvePlaceholder(text, 0, 0);
            if (placeholder != null && placeholder->IsCharacter())
            {
                var character = (Character*)placeholder;

                if (placeholder->Name != null && character->HomeWorld != 0 && character->HomeWorld != 65535)
                {
                    var world = DalamudApi.DataManager.GetExcelSheet<World>()
                        ?.FirstOrDefault(x => x.RowId == character->HomeWorld);

                    if (world != null)
                    {
                        var name = $"{ReadSeString(placeholder->Name)}@{world.Name}";
                        return name;
                    }
                }
            }

            return null;
        }

        private CharacterData ParseTextForChar(string rawText)
        {
            var character = new CharacterData();
            var placeholder = FindPlaceholder(rawText);
            if (placeholder != null)
            {
                rawText = placeholder;
            }

            rawText = rawText.Replace("'s party for", " ");

            rawText = rawText.Replace("You join", " ");
            rawText = Regex.Replace(rawText, "\\[.*?\\]", " ");
            rawText = Regex.Replace(rawText, "[^A-Za-z '-]", " ");
            rawText = string.Concat(rawText.Select(x => char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' ');
            rawText = Regex.Replace(rawText, @"\s+", " ");

            var words = rawText.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);

            var index = -1;
            for (var i = 0; index == -1 && i < this._validWorlds.Length; i++) index = Array.IndexOf(words, this._validWorlds[i]);

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
                character.WorldName = DalamudApi.ClientState.LocalPlayer?.HomeWorld.GameData?.Name;
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
                    ParseLogs(characterData, logData.data.characterData.character.Ultimates);
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

        private static string GetRegionName(string worldName)
        {
            var world = DalamudApi.DataManager.GetExcelSheet<World>()
                ?.FirstOrDefault(
                    x => x.Name.ToString().Equals(worldName, StringComparison.InvariantCultureIgnoreCase));

            if (world == null)  throw new ArgumentException("Invalid world.");

            return world?.DataCenter?.Value?.Region switch
            {
                1 => "JP",
                2 => "NA",
                3 => "EU",
                4 => "OC",
                _ => throw new ArgumentException("World not supported."),
            };
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
