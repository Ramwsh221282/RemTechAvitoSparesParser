namespace AvitoSparesParser.ParsingStages.Extensions;

public static class ParsingStageImplementation
{
    extension(ParsingStage stage)
    {
        public ParsingStage ToCatalogueStage()
        {
            return stage with { Name = ParsingStageConstants.CATALOGUE };
        }

        public ParsingStage ToConcreteItemsStage()
        {
            return stage with { Name = ParsingStageConstants.CONCRETE_ITEMS };
        }

        public ParsingStage ToFinalizationStage()
        {
            return stage with { Name = ParsingStageConstants.FINALIZATION_STAGE };
        }

        public ParsingStage ToEmptyStage()
        {
            return stage with { Name = ParsingStageConstants.EMPTY };
        }
    }
}
