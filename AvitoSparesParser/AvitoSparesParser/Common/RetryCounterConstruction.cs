namespace AvitoSparesParser.Common;

public static class RetryCounterConstruction
{
    extension(RetryCounter)
    {
        public static RetryCounter New()
        {
            return new RetryCounter(0);
        }
    }
}