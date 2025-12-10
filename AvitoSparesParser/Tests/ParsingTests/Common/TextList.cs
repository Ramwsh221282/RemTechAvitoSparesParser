namespace Tests.ParsingTests.Common;

public sealed class TextList(IEnumerable<string> text)
{
    private readonly List<string> _list = [..text];
    public void Add(TextList other) => _list.AddRange(other._list);
    public void Add(string text) => _list.Add(text);
    public void Add(params string[] texts) => _list.AddRange(texts);
    public void Add(IEnumerable<string> texts) => _list.AddRange(texts);
    public bool Empty() => _list.Count == 0;

    public async Task InvokeForEach(Func<string, Task> action)
    {
        await _list.InvokeForEach(action);
    }
}