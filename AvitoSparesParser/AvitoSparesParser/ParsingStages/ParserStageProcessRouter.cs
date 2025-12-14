using AvitoSparesParser.ParsingStages.Processes;

namespace AvitoSparesParser.ParsingStages;

public static class ParserStageProcessRouter
{
    public static ParserStageProcess ChooseRightOne(ParsingStage stage)
    {
        string stageName = stage.Name;
        return stageName switch
        {
            ParsingStageConstants.PAGINATION => ParserStageProcess.Pagination,
            ParsingStageConstants.CATALOGUE => ParserStageProcess.CatalogueItemsExtracting,
            ParsingStageConstants.CONCRETE_ITEMS => ParserStageProcess.ConcreteItems,
            _ => ParserStageProcess.Empty
        };
    }
}
