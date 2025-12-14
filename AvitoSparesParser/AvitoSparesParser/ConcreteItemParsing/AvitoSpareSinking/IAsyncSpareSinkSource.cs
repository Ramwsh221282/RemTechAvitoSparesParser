namespace AvitoSparesParser.ConcreteItemParsing.AvitoSpareSinking;

public interface IAsyncSpareSinkSource
{
    Task Write(CancellationToken ct = default);
}