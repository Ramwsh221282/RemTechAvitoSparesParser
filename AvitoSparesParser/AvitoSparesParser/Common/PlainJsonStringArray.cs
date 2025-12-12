namespace AvitoSparesParser.Common;

public sealed class PlainJsonStringArray(IEnumerable<string> entries, string json)
{
    public IReadOnlyList<string> Array { get; } = [..entries];
    public string Json { get; } = json;
}