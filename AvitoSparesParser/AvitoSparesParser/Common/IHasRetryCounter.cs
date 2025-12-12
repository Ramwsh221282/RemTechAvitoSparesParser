namespace AvitoSparesParser.Common;

public interface IHasRetryCounter
{
    RetryCounter Counter { get; }
    void IncreaseRetry() => Counter.Increase();
}