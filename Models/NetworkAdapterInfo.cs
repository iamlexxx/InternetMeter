namespace InternetMeter.Models;

public class NetworkAdapterInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public bool IsAllSources { get; init; }

    public override string ToString() => Name;
}

