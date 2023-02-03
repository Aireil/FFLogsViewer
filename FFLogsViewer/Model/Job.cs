using System.Numerics;

namespace FFLogsViewer.Model;

public class Job
{
    public string Name { get; init; } = null!;
    public Vector4 Color { get; init; }
    public string Abbreviation
    {
        get => this.abbreviation ?? this.Name;
        init => this.abbreviation = value;
    }

    private readonly string? abbreviation;
}
