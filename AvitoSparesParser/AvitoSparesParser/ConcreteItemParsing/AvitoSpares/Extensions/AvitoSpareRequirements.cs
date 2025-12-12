using AvitoSparesParser.Common;

using ParsingSDK.Parsing;

namespace AvitoSparesParser.ConcreteItemParsing.AvitoSpares.Extensions;

public sealed record AvitoSpareRequirements<T>(
    Func<T, Task<Maybe<PriceInformation>>> Prices,
    params Func<T, Task<TextList>>[] Texts);