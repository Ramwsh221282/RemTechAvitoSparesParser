using AvitoFirewallBypass;
using AvitoSparesParser.CatalogueParsing;
using PuppeteerSharp;

namespace AvitoSparesParser.ConcreteItemParsing.AvitoSpares.Extensions;

public sealed record AvitoSpareConstructionDependencies(
    AvitoCatalogueSpare CatalogueSpare,
    IBrowser Browsers,
    AvitoBypassFactory Bypasses);