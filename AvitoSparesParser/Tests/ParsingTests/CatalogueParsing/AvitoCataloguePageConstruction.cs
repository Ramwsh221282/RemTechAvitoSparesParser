using Tests.ParsingTests.Common;

namespace Tests.ParsingTests.CatalogueParsing;

public static class AvitoCataloguePageConstruction
{
    extension(AvitoCataloguePage)
    {
        public static AvitoCataloguePage New(string url)
        {
            RetryCounter counter = RetryCounter.New();
            ProcessedMarker marker = ProcessedMarker.Unprocessed();
            return new AvitoCataloguePage(
                Id: Guid.NewGuid(),
                Url: url,
                Counter: counter,
                Marker: marker
            );
        }
    }
}