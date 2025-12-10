using AvitoFirewallBypass;
using PuppeteerSharp;
using Tests.ParsingTests.CatalogueParsing;

namespace Tests.ParsingTests.ConcreteItemParsing.AvitoSpares.Extensions;

public sealed record AvitoSpareConstructionDependencies(
    AvitoCatalogueSpare CatalogueSpare,
    IBrowser Browsers,
    AvitoBypassFactory Bypasses);