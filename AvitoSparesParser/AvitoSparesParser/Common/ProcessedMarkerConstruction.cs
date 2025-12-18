namespace AvitoSparesParser.Common;

public static class ProcessedMarkerConstruction
{
    extension(ProcessedMarker)
    {
        public static ProcessedMarker Unprocessed()
        {
            return new ProcessedMarker(false);
        }
    }
}