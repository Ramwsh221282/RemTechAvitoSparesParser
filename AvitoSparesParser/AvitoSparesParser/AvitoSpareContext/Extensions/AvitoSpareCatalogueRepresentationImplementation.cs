namespace AvitoSparesParser.AvitoSpareContext.Extensions;

public static class AvitoSpareCatalogueRepresentationImplementation
{
    extension(AvitoSpareCatalogueRepresentation)
    {
        public static AvitoSpareCatalogueRepresentation Empty()
        {
            return new AvitoSpareCatalogueRepresentation(string.Empty, -1, false, string.Empty, [], string.Empty);
        }
    }
    
    extension(AvitoSpareCatalogueRepresentation representation)
    {
        public bool IsEmpty()
        {
            AvitoSpareCatalogueRepresentation empty = AvitoSpareCatalogueRepresentation.Empty();
            return representation == empty;
        }
    }
}