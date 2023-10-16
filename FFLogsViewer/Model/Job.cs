using System.Numerics;

namespace FFLogsViewer.Model;

public class Job
{
    public string Name { get; init; } = null!;
    public uint Id { get; init; }
    public Vector4 Color { get; init; }
    public string Abbreviation
    {
        get => this.abbreviation ?? this.Name;
        init => this.abbreviation = value;
    }

    private readonly string? abbreviation;

    // used in FF Logs API
    public string GetSpecName()
    {
        return this.Name.Replace(" ", string.Empty);
    }
}
