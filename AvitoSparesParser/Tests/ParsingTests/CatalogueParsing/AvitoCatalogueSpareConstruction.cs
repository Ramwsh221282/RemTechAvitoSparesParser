using Tests.ParsingTests.Common;

namespace Tests.ParsingTests.CatalogueParsing;

public static class AvitoCatalogueSpareConstruction
{
    extension(AvitoCatalogueSpare)
    {
        public static AvitoCatalogueSpare New(AvitoCatalogueItemMetadata metadata, PlainJsonStringArray photos)
        {
            ProcessedMarker marker = ProcessedMarker.Unprocessed();
            RetryCounter counter = RetryCounter.New();
            return new AvitoCatalogueSpare(metadata, photos, counter, marker);
        }

        public static AvitoCatalogueSpare MapFrom<T>(
            T source,
            Func<T, AvitoCatalogueItemMetadata> metadataMap,
            Func<T, PlainJsonStringArray> photosMap,
            Func<T, ProcessedMarker> processedMarkerMap,
            Func<T, RetryCounter> retryCounterMap
        )
        {
            return new AvitoCatalogueSpare(
                Metadata: metadataMap(source),
                Photos: photosMap(source),
                Counter: retryCounterMap(source),
                Marker: processedMarkerMap(source));
        }
    }
}