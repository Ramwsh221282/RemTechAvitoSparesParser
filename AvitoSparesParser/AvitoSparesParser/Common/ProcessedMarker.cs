namespace AvitoSparesParser.Common;

public sealed class ProcessedMarker(bool processed)
{
    public bool Processed { get; private set; } = processed;
    public void MarkProcessed()
    {
        if (Processed)
            throw new InvalidOperationException("Already marked as processed");
        Processed = true;
    }
}
