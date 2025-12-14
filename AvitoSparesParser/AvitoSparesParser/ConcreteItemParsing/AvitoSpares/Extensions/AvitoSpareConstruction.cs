using System.Text.Json;
using AvitoFirewallBypass;
using AvitoSparesParser.CatalogueParsing;
using AvitoSparesParser.CatalogueParsing.Extensions;
using AvitoSparesParser.Common;
using AvitoSparesParser.ConcreteItemParsing.AvitoWebPages;
using ParsingSDK.Parsing;
using PuppeteerSharp;

namespace AvitoSparesParser.ConcreteItemParsing.AvitoSpares.Extensions;

public static class AvitoSpareConstruction
{
    extension(AvitoSpare)
    {
        public static AvitoSpare WithJsonPayload(
            string id,
            string text,
            bool processed,
            string json
        )
        {
            AvitoSpare spare = new(id, text, new ProcessedMarker(processed));
            using JsonDocument document = JsonDocument.Parse(json);
            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                string propertyName = property.Name;
                object propertyValue = ResolveProperty(property, propertyName);
                spare.AddProperty(propertyName, propertyValue);
            }

            return spare;
        }

        public static Maybe<AvitoSpare> TryConstruct(AvitoSpare spare)
        {
            if (!ValidateProperties(spare))
                return Maybe<AvitoSpare>.None();
            return Maybe<AvitoSpare>.Some(spare);
        }

        public static async Task<Maybe<AvitoSpare>> TryExtract(
            AvitoSpareConstructionDependencies dependencies,
            AvitoSpareRequirements requirements
        )
        {
            dependencies.Deconstruct(
                out AvitoCatalogueSpare spare,
                out IBrowser browser,
                out AvitoBypassFactory bypass
            );
            requirements.Deconstruct(
                out AsyncSparePropertyFactory<AvitoSpareWebPage>[] propertyInjections
            );
            static Maybe<AvitoSpareProperty> PhotosFactory(AvitoCatalogueSpare cs) =>
                AvitoSpareProperty.Something("photos", cs.Photos.Json);

            Maybe<AvitoSpareWebPage> pageAttempt = await spare.BypassedSparePage(bypass, browser);
            if (!pageAttempt.HasValue)
                return Maybe<AvitoSpare>.None();

            AvitoSpare result = new(
                spare.Metadata.Id,
                spare.Metadata.Url,
                ProcessedMarker.Unprocessed()
            );
            result.AddProperty(spare, PhotosFactory);
            foreach (AsyncSparePropertyFactory<AvitoSpareWebPage> requirement in propertyInjections)
                await result.AddProperty(pageAttempt.Value, requirement);

            return TryConstruct(result);
        }
    }

    private static object ResolveProperty(JsonProperty property, string propertyName)
    {
        return propertyName switch
        {
            "type" => property.Value.GetString()!,
            "title" => property.Value.GetString()!,
            "oem" => property.Value.GetString()!,
            "price" => property.Value.GetInt64(),
            "is_nds" => property.Value.GetBoolean(),
            "photos" => property.Value.GetRawText(),
            "address" => property.Value.GetString()!,
            _ => throw new InvalidOperationException("Unknown property name: " + propertyName),
        };
    }

    private static bool ValidateProperties(AvitoSpare spare)
    {
        if (!spare.Read<long>("price").HasValue)
            return false;
        if (!spare.Read<string>("title").HasValue)
            return false;
        if (!spare.Read<string>("oem").HasValue)
            return false;
        if (!spare.Read<string>("type").HasValue)
            return false;
        return true;
    }
}
