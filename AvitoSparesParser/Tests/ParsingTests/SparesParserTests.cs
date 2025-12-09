using System.Text.Json;

namespace Tests.ParsingTests;

public sealed class RetryCounter(int counter)
{
    public int Value { get; private set; } = counter;
    public void Increase()
    {
        int current = Value;
        int next = current + 1;
        Value = next;
    }
}

public interface IRetriable
{
    RetryCounter Counter { get; }
    void IncreaseRetry() => Counter.Increase();
}

public interface IProcessMarkable
{
    ProcessedMarker Marker { get; }
    void MarkProcessed() => Marker.MarkProcessed();
}

public sealed class ProcessedMarker(bool processed)
{
    public bool Processed { get; private set; } = processed;
    public void MarkProcessed()
    {
        if (Processed)
            throw new InvalidOperationException("Already marked as processed");
        Processed = true;
    }
}

public sealed class AvitoCatalogueItemMetadata(string id, string url, string oem)
{
    public string Id { get; } = id;
    public string Url { get; } = url;
    public string Oem { get; } = oem;
}

public sealed class PlainJsonStringArray
{
    public Lazy<IReadOnlyList<string>> Array { get; }
    public Lazy<string> Json { get; }

    public PlainJsonStringArray(JsonDocument document)
    {
        Array = new Lazy<IReadOnlyList<string>>(() => StringFromJson(document));
        Json = new Lazy<string>(() => PlainJsonFromDocument(document));
    }

    private static string PlainJsonFromDocument(JsonDocument document)
    {
        return document.RootElement.GetRawText();
    }
    
    private static IReadOnlyList<string> StringFromJson(JsonDocument document)
    {
        int length = document.RootElement.GetArrayLength();
        List<string> strings = new(length);
        foreach (JsonElement @string in document.RootElement.EnumerateArray())
            strings.Add(@string.GetString()!);
        return strings;
    }
}

public sealed record PriceInformation(long Value, bool IsNds);

public sealed record AvitoCatalogueSpare(
    AvitoCatalogueItemMetadata Metadata, 
    RetryCounter Counter, 
    ProcessedMarker Marker) : 
    IRetriable, 
    IProcessMarkable;

public sealed record AvitoSpare(
    AvitoCatalogueSpare Spare,
    PriceInformation Price,
    PlainJsonStringArray Photos,
    string Type,
    string Address);



public sealed class SparesParserTests(SparesParsingFixture fixture) : IClassFixture<SparesParsingFixture>
{
    private readonly IServiceProvider _sp = fixture.Services;

    [Fact]
    private async Task Parse_Single_Advertisement()
    {
        
    }
}