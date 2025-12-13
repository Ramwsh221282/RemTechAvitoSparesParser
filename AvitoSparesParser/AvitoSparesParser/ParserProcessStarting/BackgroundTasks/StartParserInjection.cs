namespace AvitoSparesParser.ParserProcessStarting.BackgroundTasks;

public static class StartParserInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterStartParserListener()
        {
            services.AddHostedService<StartParserProcessListener>();
        }
    }
}
