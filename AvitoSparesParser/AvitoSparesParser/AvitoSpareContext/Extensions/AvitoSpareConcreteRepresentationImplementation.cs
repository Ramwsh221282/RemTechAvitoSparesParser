namespace AvitoSparesParser.AvitoSpareContext.Extensions;

public static class AvitoSpareConcreteRepresentationImplementation
{
    extension(AvitoSpareConcreteRepresentation)
    {
        public static AvitoSpareConcreteRepresentation Empty()
        {
            return new(string.Empty, string.Empty);
        }
    }
    
    extension(AvitoSpareConcreteRepresentation representation)
    {
        public bool IsEmpty()
        {
            AvitoSpareConcreteRepresentation empty = AvitoSpareConcreteRepresentation.Empty();
            return representation == empty;
        }
    }
}