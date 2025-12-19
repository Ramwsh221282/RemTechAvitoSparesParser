using AvitoSparesParser.AvitoSpareContext.Extensions;

namespace AvitoSparesParser.AvitoSpareContext;

public sealed class AvitoSpare
{
    private AvitoSpare() { }
    public string Id { get; private init; } = string.Empty;
    public int RetryCount { get; init; } = 0;
    public bool Processed { get; init; } = false;
    public AvitoSpareCatalogueRepresentation CatalogueRepresentation { get; private init; } = AvitoSpareCatalogueRepresentation.Empty();
    public AvitoSpareConcreteRepresentation ConcreteRepresentation { get; private init; } = AvitoSpareConcreteRepresentation.Empty();

    public AvitoSpare Transform<T>(
        T source,
        Func<T, AvitoSpareCatalogueRepresentation>? catalogueRepresentationExtractor = null, 
        Func<T, AvitoSpareConcreteRepresentation>? concreteRepresentationExtractor = null,
        Func<T, int>? retryCountExtractor = null,
        Func<T, bool>? processedExtractor = null)
    {
        AvitoSpare current = this;
        return new AvitoSpare()
        {
            Id = current.Id,
            CatalogueRepresentation = catalogueRepresentationExtractor?.Invoke(source) ?? current.CatalogueRepresentation,
            ConcreteRepresentation = concreteRepresentationExtractor?.Invoke(source) ?? current.ConcreteRepresentation,
            RetryCount = retryCountExtractor?.Invoke(source) ?? current.RetryCount,
            Processed = processedExtractor?.Invoke(source) ?? current.Processed,
        };
    }

    public AvitoSpare Concretized(AvitoSpareConcreteRepresentation concreteRepresentation)
    {
        return Transform(concreteRepresentation, concreteRepresentationExtractor: r => r);
    }

    public AvitoSpare IncreaseRetryAmount()
    {
        int current = RetryCount;
        int next = current + 1;
        return Transform(next, retryCountExtractor: n => n);
    }

    public AvitoSpare MarkProcessed()
    {
        bool processed = true;
        return Transform(processed, processedExtractor: p => p);
    }
    
    public static AvitoSpare Create(
        string id,
        int retryCount,
        bool processed,
        AvitoSpareCatalogueRepresentation catalogueRepresentation, 
        AvitoSpareConcreteRepresentation concreteRepresentation) => new()
    {
        Id = id,
        RetryCount = retryCount,
        Processed = processed,
        CatalogueRepresentation = catalogueRepresentation,
        ConcreteRepresentation = concreteRepresentation,
    };
    
    public static AvitoSpare Identified(string id) => new() { Id = id };
    
    public static AvitoSpare CatalogueRepresented(string id, AvitoSpareCatalogueRepresentation catalogueRepresentation) =>
        new() { Id = id, CatalogueRepresentation = catalogueRepresentation };
    
    public static AvitoSpare ConcreteRepresented(string id, AvitoSpareConcreteRepresentation concreteRepresentation) =>
        new() { Id = id, ConcreteRepresentation = concreteRepresentation };
}