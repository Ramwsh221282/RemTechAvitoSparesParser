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
        if (catalogueRepresentationExtractor != null)
            current = new AvitoSpare() { Id = current.Id, CatalogueRepresentation = catalogueRepresentationExtractor(source) };
        if (concreteRepresentationExtractor != null)
            current = new AvitoSpare() { Id = current.Id, ConcreteRepresentation = concreteRepresentationExtractor(source) };
        if (retryCountExtractor != null)
            current = new AvitoSpare() { Id = current.Id, RetryCount = retryCountExtractor(source) };
        if (processedExtractor != null)
            current = new AvitoSpare() { Id = current.Id, Processed = processedExtractor(source) };
        return current;
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
        AvitoSpareCatalogueRepresentation catalogueRepresentation, 
        AvitoSpareConcreteRepresentation concreteRepresentation) => new()
    {
        Id = id,
        CatalogueRepresentation = catalogueRepresentation,
        ConcreteRepresentation = concreteRepresentation,
    };
    
    public static AvitoSpare Identified(string id) => new() { Id = id };
    
    public static AvitoSpare CatalogueRepresented(string id, AvitoSpareCatalogueRepresentation catalogueRepresentation) =>
        new() { Id = id, CatalogueRepresentation = catalogueRepresentation };
    
    public static AvitoSpare ConcreteRepresented(string id, AvitoSpareConcreteRepresentation concreteRepresentation) =>
        new() { Id = id, ConcreteRepresentation = concreteRepresentation };
}