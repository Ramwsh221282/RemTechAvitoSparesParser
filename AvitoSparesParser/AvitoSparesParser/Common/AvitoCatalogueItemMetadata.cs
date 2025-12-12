namespace AvitoSparesParser.Common;

public sealed class AvitoCatalogueItemMetadata(string id, string url)
{
    public string Id { get; } = id;
    public string Url { get; } = url;
}