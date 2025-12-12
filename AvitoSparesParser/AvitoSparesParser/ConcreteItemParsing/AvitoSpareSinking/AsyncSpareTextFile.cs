namespace AvitoSparesParser.ConcreteItemParsing.AvitoSpareSinking;

public sealed class AsyncSpareTextFile(string contents, string path) : IAsyncSpareSinkSource
{
    public async Task Write(CancellationToken ct = default)
    {
        using StreamWriter writer = File.CreateText(path);
        await writer.WriteLineAsync(contents);
        await writer.FlushAsync(ct);
    }
}