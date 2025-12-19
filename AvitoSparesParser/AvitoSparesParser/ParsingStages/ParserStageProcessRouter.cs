using AvitoSparesParser.ParsingStages.Processes;

namespace AvitoSparesParser.ParsingStages;

public static class ParserStageProcessRouter
{
    public static ParserStageProcess ResolveStageByName(ParsingStage stage)
    {
        string stageName = stage.Name;
        return stageName switch
        {
            ParsingStageConstants.PAGINATION => ParserStageProcess.Pagination,
            ParsingStageConstants.CATALOGUE => ParserStageProcess.CatalogueItemsExtracting,
            ParsingStageConstants.CONCRETE_ITEMS => ParserStageProcess.ConcreteItems,
            ParsingStageConstants.FINALIZATION_STAGE => ParserStageProcess.Finalization,
            _ => ParserStageProcess.Empty
        };
    }
}
