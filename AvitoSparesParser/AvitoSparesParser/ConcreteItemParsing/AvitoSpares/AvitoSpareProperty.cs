using ParsingSDK.Parsing;

namespace AvitoSparesParser.ConcreteItemParsing.AvitoSpares;

public delegate Task<Maybe<AvitoSpareProperty>> AsyncSparePropertyFactory<T>(T source) where T : notnull;
public delegate Maybe<AvitoSpareProperty> SparePropertyFactory<T>(T source) where T : notnull;
public sealed record AvitoSpareProperty(string Name, object Value);

public static class AvitoSparePropertyImplementation
{
    extension(AvitoSpareProperty)
    {
        public static Maybe<AvitoSpareProperty> Nothing()
        {
            return Maybe<AvitoSpareProperty>.None();
        }

        public static Maybe<AvitoSpareProperty> Something<T>(string name, T value) where T : notnull
        {
            return Maybe<AvitoSpareProperty>.Some(new AvitoSpareProperty(name, value));
        }
    }
}
