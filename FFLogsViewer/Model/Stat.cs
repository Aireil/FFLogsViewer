using System;

namespace FFLogsViewer.Model;

public class Stat
{
    public string Alias = string.Empty;
    public string Name = null!;
    public StatType Type;
    public bool IsEnabled;

    public string GetFinalAlias(string metricAbbreviation)
    {
        if (this.Type == StatType.BestAmount &&
            this.Alias.Equals("/metric/", StringComparison.OrdinalIgnoreCase))
        {
            return metricAbbreviation;
        }

        return this.Alias != string.Empty ? this.Alias : this.Name;
    }
}
