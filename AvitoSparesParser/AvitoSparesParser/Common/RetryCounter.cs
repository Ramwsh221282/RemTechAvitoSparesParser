namespace AvitoSparesParser.Common;

public sealed class RetryCounter(int counter)
{
    public int Value { get; private set; } = counter;
    public void Increase()
    {
        int current = Value;
        int next = current + 1;
        Value = next;
    }
}

public static class RetryCounterExtensions
{
    extension(IHasRetryCounter)
    {
        public static void Increase(IHasRetryCounter counter)
        {
            counter.IncreaseRetry();
        }
    }
}