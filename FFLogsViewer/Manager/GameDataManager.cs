using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Logging;
using FFLogsViewer.Model;
using FFLogsViewer.Model.GameData;

namespace FFLogsViewer.Manager;

public class GameDataManager
{
    public static readonly List<Metric> AvailableMetrics = new()
    {
        new Metric { Name = "rDPS", InternalName = "rdps" },
        new Metric { Name = "aDPS", InternalName = "dps" },
        new Metric { Name = "nDPS", InternalName = "ndps" },
        new Metric { Name = "HPS", InternalName = "hps" },
        new Metric { Name = "Healer Combined rDPS", InternalName = "healercombinedrdps" },
        new Metric { Name = "Healer Combined aDPS", InternalName = "healercombineddps" },
        new Metric { Name = "Healer Combined nDPS", InternalName = "healercombinedndps" },
        new Metric { Name = "Tank Combined rDPS", InternalName = "tankcombinedrdps" },
        new Metric { Name = "Tank Combined aDPS", InternalName = "tankcombineddps" },
        new Metric { Name = "Tank Combined nDPS", InternalName = "tankcombinedndps" },
    };
    public bool IsDataReady;
    public bool IsDataLoading;
    public bool HasFailed;
    public GameData? GameData;
    public List<Job> Jobs;

    public GameDataManager()
    {
        this.Jobs = GetJobs();
    }

    public static Job GetDefaultJob()
    {
        return new Job { Name = "All jobs", Color = new Vector4(255, 255, 255, 255) };
    }

    public void FetchData()
    {
        if (this.IsDataLoading) return;

        this.IsDataReady = false;
        this.IsDataLoading = true;
        Task.Run(async () =>
        {
            await Service.FfLogsClient.FetchGameData().ConfigureAwait(false);
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
                PluginLog.Error(e, "Networking error.");
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
            new() { Name = "Astrologian", Color = new Vector4(255, 231, 74, 255) / 255 },
            new() { Name = "Bard", Color = new Vector4(145, 150, 186, 255) / 255 },
            new() { Name = "Black Mage", Color = new Vector4(165, 121, 214, 255) / 255 },
            new() { Name = "Dancer", Color = new Vector4(226, 176, 175, 255) / 255 },
            new() { Name = "Dark Knight", Color = new Vector4(209, 38, 204, 255) / 255 },
            new() { Name = "Dragoon", Color = new Vector4(65, 100, 205, 255) / 255 },
            new() { Name = "Gunbreaker", Color = new Vector4(121, 109, 48, 255) / 255 },
            new() { Name = "Machinist", Color = new Vector4(110, 225, 214, 255) / 255 },
            new() { Name = "Monk", Color = new Vector4(214, 156, 0, 255) / 255 },
            new() { Name = "Ninja", Color = new Vector4(175, 25, 100, 255) / 255 },
            new() { Name = "Paladin", Color = new Vector4(168, 210, 230, 255) / 255 },
            new() { Name = "Red Mage", Color = new Vector4(232, 123, 123, 255) / 255 },
            new() { Name = "Reaper", Color = new Vector4(150, 90, 144, 255) / 255 },
            new() { Name = "Sage", Color = new Vector4(128, 160, 240, 255) / 255 },
            new() { Name = "Samurai", Color = new Vector4(228, 109, 4, 255) / 255 },
            new() { Name = "Scholar", Color = new Vector4(134, 87, 255, 255) / 255 },
            new() { Name = "Summoner", Color = new Vector4(45, 155, 120, 255) / 255 },
            new() { Name = "Warrior", Color = new Vector4(207, 38, 33, 255) / 255 },
            new() { Name = "White Mage", Color = new Vector4(255, 240, 220, 255) / 255 },
        };
    }
}
