namespace AvitoSparesParser.AvitoSpareContext;

public sealed record AvitoSpareCatalogueRepresentation(
    string Url,
    long Price,
    bool IsNds,
    string Address,
    string[] Photos,
    string Oem
);