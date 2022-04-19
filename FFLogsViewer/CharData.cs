using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Logging;
using FFLogsViewer.Manager;
using FFLogsViewer.Model;
using ImGuiNET;

namespace FFLogsViewer;

public class CharData
{
    public Job Job = GameDataManager.GetDefaultJob();
    public Metric? OverriddenMetric;
    public Metric? LoadedMetric;
    public string FirstName = string.Empty;
    public string LastName = string.Empty;
    public string WorldName = string.Empty;
    public string RegionName = string.Empty;
    public string LoadedFirstName = string.Empty;
    public string LoadedLastName = string.Empty;
    public string LoadedWorldName = string.Empty;
    public bool IsDataLoading;
    public bool IsDataReady;

    public List<Encounter> Encounters = new();

    public void SetInfo(string firstName, string lastName, string worldName)
    {
        this.FirstName = firstName;
        this.LastName = lastName;
        this.WorldName = worldName;
    }

    public bool SetInfo(PlayerCharacter playerCharacter)
    {
        if (playerCharacter.HomeWorld.GameData?.Name == null)
        {
            Service.MainWindow.SetErrorMessage("An error occured, please try again");
            PluginLog.Error("SetInfo character world was null");
            return false;
        }

        this.FirstName = playerCharacter.Name.TextValue.Split(' ')[0];
        this.LastName = playerCharacter.Name.TextValue.Split(' ')[1];
        this.WorldName = playerCharacter.HomeWorld.GameData.Name.ToString();
        return true;
    }

    public bool IsInfoSet()
    {
        return this.FirstName != string.Empty && this.LastName != string.Empty && this.WorldName != string.Empty;
    }

    public void FetchTargetChar()
    {
        var target = Service.TargetManager.Target;
        if (target is PlayerCharacter targetCharacter && target.ObjectKind != ObjectKind.Companion)
        {
            if (this.SetInfo(targetCharacter))
            {
                this.FetchData();
            }
        }
        else
        {
            Service.MainWindow.SetErrorMessage("Not a valid target");
        }
    }

    public void FetchData()
    {
        if (this.IsDataLoading)
        {
            return;
        }

        Service.MainWindow.SetErrorMessage(string.Empty);

        if (!this.IsInfoSet())
        {
            Service.MainWindow.SetErrorMessage("Please fill first name, last name, and world");
            return;
        }

        var regionName = CharDataManager.GetRegionName(this.WorldName);
        if (regionName == null)
        {
            Service.MainWindow.SetErrorMessage("World not supported or invalid");
            return;
        }

        this.RegionName = regionName;

        this.IsDataLoading = true;
        this.ResetData();
        Task.Run(async () =>
        {
            var rawData = await Service.FfLogsClient.FetchLogs(this).ConfigureAwait(false);
            if (rawData == null)
            {
                this.IsDataLoading = false;
                Service.MainWindow.SetErrorMessage("Could not reach FF Logs servers");
                PluginLog.Error("rawData is null");
                return;
            }

            if (rawData.data?.characterData?.character == null)
            {
                if (rawData.error != null && rawData.error == "Unauthenticated.")
                {
                    this.IsDataLoading = false;
                    Service.MainWindow.SetErrorMessage("API Client not valid, check config");
                    PluginLog.Log($"Unauthenticated: {rawData}");
                    return;
                }

                if (rawData.errors != null)
                {
                    this.IsDataLoading = false;
                    Service.MainWindow.SetErrorMessage("Malformed GraphQL query.");
                    PluginLog.Log($"Malformed GraphQL query: {rawData}");
                    return;
                }

                this.IsDataLoading = false;
                Service.MainWindow.SetErrorMessage("Character not found on FF Logs");
                return;
            }

            var character = rawData.data.characterData.character;

            if (character.hidden == "true")
            {
                this.IsDataLoading = false;
                Service.MainWindow.SetErrorMessage(
                    $"{this.FirstName} {this.LastName}@{this.WorldName}'s logs are hidden");
                return;
            }

            this.Encounters = new List<Encounter>();

            var properties = character.Properties();
            foreach (var prop in properties)
            {
                if (prop.Name != "hidden")
                {
                    this.ParseZone(prop.Value);
                }
            }

            this.IsDataReady = true;
            this.LoadedFirstName = this.FirstName;
            this.LoadedLastName = this.LastName;
            this.LoadedWorldName = this.WorldName;
        }).ContinueWith(t =>
        {
            this.IsDataLoading = false;
            if (!t.IsFaulted) return;
            if (t.Exception == null) return;
            Service.MainWindow.SetErrorMessage("Networking error, please try again");
            foreach (var e in t.Exception.Flatten().InnerExceptions)
            {
                PluginLog.Error(e, "Networking error");
            }
        });
    }

    public bool ParseTextForChar(string rawText)
    {
        var character = new CharData();
        var placeholder = CharDataManager.FindPlaceholder(rawText);
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

        var words = rawText.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

        var index = -1;
        for (var i = 0; index == -1 && i < Service.CharDataManager.ValidWorlds.Length; i++) index = Array.IndexOf(words, Service.CharDataManager.ValidWorlds[i]);

        if (index - 2 >= 0)
        {
            character.FirstName = words[index - 2];
            character.LastName = words[index - 1];
            character.WorldName = words[index];
        }
        else if (words.Length >= 2)
        {
            if (Service.ClientState.LocalPlayer?.HomeWorld.GameData?.Name == null)
            {
                return false;
            }

            character.FirstName = words[0];
            character.LastName = words[1];
            character.WorldName = Service.ClientState.LocalPlayer.HomeWorld.GameData.Name.ToString();
        }
        else
        {
            return false;
        }

        if (!char.IsUpper(character.FirstName[0]) || !char.IsUpper(character.LastName[0]))
        {
            return false;
        }

        this.FirstName = character.FirstName;
        this.LastName = character.LastName;
        this.WorldName = character.WorldName;

        return true;
    }

    public void FetchClipboardCharacter()
    {
        string clipboardRawText;
        try
        {
            if (ImGui.GetClipboardText() == null)
            {
                Service.MainWindow.SetErrorMessage("Couldn't get clipboard text");
                return;
            }

            clipboardRawText = ImGui.GetClipboardText();
        }
        catch
        {
            Service.MainWindow.SetErrorMessage("Couldn't get clipboard text");
            return;
        }

        this.FetchTextCharacter(clipboardRawText);
    }

    public void FetchTextCharacter(string text)
    {
        if (!this.ParseTextForChar(text))
        {
            Service.MainWindow.SetErrorMessage("No character found.");
            return;
        }

        this.FetchData();
    }

    public void ResetData()
    {
        this.Encounters = new List<Encounter>();
        this.IsDataReady = false;
        this.LoadedMetric = null;
    }

    private void ParseZone(dynamic zone)
    {
        if (zone.rankings == null) return;
        foreach (var ranking in zone.rankings)
        {
            if (ranking.encounter == null) continue;

            var encounter = new Encounter
            {
                Id = ranking.encounter.id,
                Difficulty = zone.difficulty,
                Metric = zone.metric,
            };

            if (ranking.spec != null)
            {
                encounter.Best = Convert.ToInt32(Math.Floor((float)ranking.rankPercent));
                encounter.Median = Convert.ToInt32(Math.Floor((float)ranking.medianPercent));
                encounter.Kills = ranking.totalKills;
                encounter.Fastest = ranking.fastestKill;
                encounter.BestAmount = ranking.bestAmount;
                var jobName = Regex.Replace(ranking.spec.ToString(), "([a-z])([A-Z])", "$1 $2");
                encounter.Job = Service.GameDataManager.Jobs.FirstOrDefault(job => job.Name == jobName);
                var bestJobName = Regex.Replace(ranking.bestSpec.ToString(), "([a-z])([A-Z])", "$1 $2");
                encounter.BestJob = Service.GameDataManager.Jobs.FirstOrDefault(job => job.Name == bestJobName);
            }

            this.Encounters.Add(encounter);
        }
    }
}
