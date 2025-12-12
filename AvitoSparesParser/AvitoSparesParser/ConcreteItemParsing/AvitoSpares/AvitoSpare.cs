using AvitoSparesParser.CatalogueParsing;
using AvitoSparesParser.Common;
using AvitoSparesParser.ConcreteItemParsing.AvitoSpareSinking;

namespace AvitoSparesParser.ConcreteItemParsing.AvitoSpares;

public sealed record AvitoSpare(AvitoCatalogueSpare Spare, PriceInformation Price, TextList Texts)
{
    public async Task WriteUsing(Func<AvitoSpare, IAsyncSpareSinkSource> sinkFn, CancellationToken ct = default)
    {
        IAsyncSpareSinkSource sink = sinkFn(this);
        await sink.Write(ct);
    }

    public async Task WriteTextsUsing(Func<string, Task> writeFn)
    {
        await Texts.InvokeForEach(writeFn);
    }
    
    public async Task WriteUsing(Func<AvitoSpare, Task> sinkFn)
    {
        await sinkFn(this);
    }
}