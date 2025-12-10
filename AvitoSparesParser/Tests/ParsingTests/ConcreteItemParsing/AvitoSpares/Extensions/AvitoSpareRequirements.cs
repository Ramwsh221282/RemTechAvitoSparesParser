using ParsingSDK.Parsing;
using Tests.ParsingTests.Common;

namespace Tests.ParsingTests.ConcreteItemParsing.AvitoSpares.Extensions;

public sealed record AvitoSpareRequirements<T>(
    Func<T, Task<Maybe<PriceInformation>>> Prices,
    params Func<T, Task<TextList>>[] Texts);