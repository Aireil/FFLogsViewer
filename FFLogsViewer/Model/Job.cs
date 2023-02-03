using System.Numerics;

namespace FFLogsViewer.Model;

public class Job
{
    public string Name { get; init; } = null!;
    public string Abbreviation { get; init; } = null!;
    public Vector4 Color { get; init; }
}
