using RemTech.SharedKernel.Infrastructure.Quartz;

namespace AvitoSparesParser.ParsingStages;

public static class ParsingStagesInjection
{
    extension(IServiceCollection services)
    {
        public void RegisterParserWorkStages()
        {
            services.AddTransient<ICronScheduleJob, ParsingStageBackgroundInvoker>();
            services.AddSingleton<ParserStageDependencies>();
        }
    }
}
