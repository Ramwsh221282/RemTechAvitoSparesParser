namespace Tests.ParsingTests;

public sealed class RetryCounter(int initial)
{
    public int Counter { get; private set; } = initial;
    
    public void Increase() => Counter += 1;
}

public sealed class ProcessedMarker(bool value)
{
    public bool Processed { get; private set; } = value;

    public void MarkProcessed()
    {
        if (Processed)
            throw new InvalidOperationException("Already marked as processed");
        Processed = true;
    }
}

public sealed class AvitoCatalogueItemMetadata
{
    
}

public sealed record AvitoCatalogueSpare(
    string Id,
    string Url,
    string Oem,
    bool WasProcessed,
    int RetryCount
    );

public sealed record AvitoSpare(
    string Id,
    string Url,
    string Type,
    string Oem,
    long Price,
    bool IsNds,
    string Address);



public sealed class SparesParserTests(SparesParsingFixture fixture) : IClassFixture<SparesParsingFixture>
{
    private readonly IServiceProvider _sp = fixture.Services;

    [Fact]
    private async Task Parse_Single_Advertisement()
    {
        
    }
}