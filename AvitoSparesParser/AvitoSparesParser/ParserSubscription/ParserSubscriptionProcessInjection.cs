namespace AvitoSparesParser.ParserSubscription;

public static class ParserSubscriptionProcessInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterParserSubscriptionProcess()
        {
            services.AddTransient<ParserSubscriptionProcess>();
        }
    }
}
