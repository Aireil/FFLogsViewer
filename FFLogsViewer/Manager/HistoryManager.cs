using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FFLogsViewer.Model;
using Newtonsoft.Json;

namespace FFLogsViewer.Manager;

public class HistoryManager
{
    public List<HistoryEntry> History { get; set; } = [];
    private readonly object historyLock = new();

    public HistoryManager()
    {
        this.LoadHistory();
    }

    public void AddHistoryEntry(CharData charData)
    {
        lock (this.historyLock)
        {
            var historyEntry = this.History.FirstOrDefault(entry => entry.FirstName == charData.FirstName && entry.LastName == charData.LastName && entry.WorldName == charData.WorldName);
            if (historyEntry != null)
            {
                historyEntry.LastSeen = DateTime.Now;
            }
            else
            {
                this.History.Add(new HistoryEntry
                {
                    FirstName = charData.FirstName,
                    LastName = charData.LastName,
                    WorldName = charData.WorldName,
                });
            }

            this.History.Sort((x, y) => DateTime.Compare(y.LastSeen, x.LastSeen));

            const int maxHistory = 50;
            if (this.History.Count > maxHistory)
            {
                this.History.RemoveAt(maxHistory);
            }

            this.SaveHistory();
        }
    }

    private static string GetHistoryPath()
    {
        return Path.Combine(Service.Interface.ConfigDirectory.FullName, "history.json");
    }

    private void LoadHistory()
    {
        if (File.Exists(GetHistoryPath()))
        {
            this.History = JsonConvert.DeserializeObject<List<HistoryEntry>>(File.ReadAllText(GetHistoryPath())) ?? [];
        }
    }

    private void SaveHistory()
    {
        File.WriteAllText(GetHistoryPath(), JsonConvert.SerializeObject(this.History));
    }
}
