namespace Tests.ParsingTests.Common;

public interface IHasProcessedMarker
{
    ProcessedMarker Marker { get; }
    void MarkProcessed() => Marker.MarkProcessed();
}