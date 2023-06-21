using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Logging;
using FFLogsViewer.Model;
using FFLogsViewer.Model.GameData;

namespace FFLogsViewer.Manager;

public class GameDataManager : IDisposable
{
    public static readonly List<Metric> AvailableMetrics = new()
    {
        new Metric { Name = "rDPS", InternalName = "rdps" },
        new Metric { Name = "aDPS", InternalName = "dps" },
        new Metric { Name = "nDPS", InternalName = "ndps" },
        new Metric { Name = "cDPS", InternalName = "cdps" },
        new Metric { Name = "HPS", InternalName = "hps" },
        new Metric { Name = "Healer Combined rDPS", Abbreviation = "HC rDPS", InternalName = "healercombinedrdps" },
        new Metric { Name = "Healer Combined aDPS", Abbreviation = "HC aDPS", InternalName = "healercombineddps" },
        new Metric { Name = "Healer Combined nDPS", Abbreviation = "HC nDPS", InternalName = "healercombinedndps" },
        new Metric { Name = "Tank Combined rDPS", Abbreviation = "TC rDPS", InternalName = "tankcombinedrdps" },
        new Metric { Name = "Tank Combined aDPS", Abbreviation = "TC aDPS", InternalName = "tankcombineddps" },
        new Metric { Name = "Tank Combined nDPS", Abbreviation = "TC nDPS", InternalName = "tankcombinedndps" },
    };

    public static readonly List<Partition> AvailablePartitions = new()
    {
        new Partition { Name = "Standard", Abbreviation = "S", Id = -1 },
        new Partition { Name = "Non-Standard", Abbreviation = "N-S", Id = -2 },
    };

    public volatile bool IsDataReady;
    public volatile bool IsDataLoading;
    public volatile bool HasFailed;
    public GameData? GameData;
    public List<Job> Jobs;
    public JobIconsManager JobIconsManager;

    public GameDataManager()
    {
        this.Jobs = GetJobs();
        this.JobIconsManager = new JobIconsManager();
    }

    public static Job GetDefaultJob()
    {
        return new Job { Name = "All jobs", Color = new Vector4(255, 255, 255, 255) };
    }

    public static Partition GetDefaultPartition()
    {
        return AvailablePartitions[0];
    }

    public void Dispose()
    {
        this.JobIconsManager.Dispose();

        GC.SuppressFinalize(this);
    }

    public void FetchData()
    {
        if (this.IsDataLoading)
        {
            return;
        }

        this.IsDataReady = false;
        this.IsDataLoading = true;
        Task.Run(async () =>
        {
            await Service.FFLogsClient.FetchGameData().ConfigureAwait(false);
        }).ContinueWith(t =>
        {
            if (!this.IsDataReady)
            {
                this.HasFailed = true;
            }

            this.IsDataLoading = false;
            if (!t.IsFaulted) return;
            if (t.Exception == null) return;
            foreach (var e in t.Exception.Flatten().InnerExceptions)
            {
                PluginLog.Error(e, "Network error.");
            }
        });
    }

    public void SetDataFromJson(string jsonContent)
    {
        var gameData = GameData.FromJson(jsonContent);
        if (gameData == null)
        {
            PluginLog.Error("gameData was null while fetching game data");
        }
        else if (gameData.Errors == null)
        {
            if (gameData.IsDataValid())
            {
                this.GameData = gameData;
                this.IsDataReady = true;
            }
        }
        else
        {
            PluginLog.Error("Errors while fetching game data: " + gameData.Errors.Message);
        }
    }

    private static List<Job> GetJobs()
    {
        return new List<Job>
        {
            GetDefaultJob(),
            new() { Name = "Astrologian", Abbreviation = "AST", Color = new Vector4(255, 231, 74, 255) / 255 },
            new() { Name = "Bard", Abbreviation = "BRD", Color = new Vector4(145, 150, 186, 255) / 255 },
            new() { Name = "Black Mage", Abbreviation = "BLM", Color = new Vector4(165, 121, 214, 255) / 255 },
            new() { Name = "Dancer", Abbreviation = "DNC", Color = new Vector4(226, 176, 175, 255) / 255 },
            new() { Name = "Dark Knight", Abbreviation = "DRK", Color = new Vector4(209, 38, 204, 255) / 255 },
            new() { Name = "Dragoon", Abbreviation = "DRG", Color = new Vector4(65, 100, 205, 255) / 255 },
            new() { Name = "Gunbreaker", Abbreviation = "GNB", Color = new Vector4(121, 109, 48, 255) / 255 },
            new() { Name = "Machinist", Abbreviation = "MCH", Color = new Vector4(110, 225, 214, 255) / 255 },
            new() { Name = "Monk", Abbreviation = "MNK", Color = new Vector4(214, 156, 0, 255) / 255 },
            new() { Name = "Ninja", Abbreviation = "NIN", Color = new Vector4(175, 25, 100, 255) / 255 },
            new() { Name = "Paladin", Abbreviation = "PLD", Color = new Vector4(168, 210, 230, 255) / 255 },
            new() { Name = "Red Mage", Abbreviation = "RDM", Color = new Vector4(232, 123, 123, 255) / 255 },
            new() { Name = "Reaper", Abbreviation = "RPR", Color = new Vector4(150, 90, 144, 255) / 255 },
            new() { Name = "Sage", Abbreviation = "SGE", Color = new Vector4(128, 160, 240, 255) / 255 },
            new() { Name = "Samurai", Abbreviation = "SAM", Color = new Vector4(228, 109, 4, 255) / 255 },
            new() { Name = "Scholar", Abbreviation = "SCH", Color = new Vector4(134, 87, 255, 255) / 255 },
            new() { Name = "Summoner", Abbreviation = "SMN", Color = new Vector4(45, 155, 120, 255) / 255 },
            new() { Name = "Warrior", Abbreviation = "WAR", Color = new Vector4(207, 38, 33, 255) / 255 },
            new() { Name = "White Mage", Abbreviation = "WHM", Color = new Vector4(255, 240, 220, 255) / 255 },
        };
    }
}
