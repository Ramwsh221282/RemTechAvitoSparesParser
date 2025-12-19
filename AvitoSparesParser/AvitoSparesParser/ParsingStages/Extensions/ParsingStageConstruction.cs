using AvitoSparesParser.ParserStartConfiguration;

namespace AvitoSparesParser.ParsingStages.Extensions;

public static class ParsingStageConstruction
{
    extension(ParsingStage)
    {
        public static ParsingStage PaginationFromParser(ProcessingParser parser)
        {
            return new ParsingStage(parser.Id, ParsingStageConstants.PAGINATION);
        }
    }
}
