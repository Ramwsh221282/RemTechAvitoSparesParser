using AvitoSparesParser.ConcreteItemParsing.AvitoWebPages;

namespace AvitoSparesParser.ConcreteItemParsing.AvitoSpares;

public sealed record AvitoSpareRequirements(params AsyncSparePropertyFactory<AvitoSpareWebPage>[] PropertySources);
