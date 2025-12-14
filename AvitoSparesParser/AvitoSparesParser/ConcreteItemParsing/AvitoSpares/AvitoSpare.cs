using System.Text.Json;
using AvitoSparesParser.Common;
using ParsingSDK.Parsing;

namespace AvitoSparesParser.ConcreteItemParsing.AvitoSpares;

public sealed class AvitoSpare(string id, string url, ProcessedMarker marker) : IHasProcessedMarker
{
    private readonly Dictionary<string, object> _properties = [];
    public string Id { get; } = id;
    public string Url { get; } = url;
    public ProcessedMarker Marker { get; } = marker;
    public string Payload => JsonSerializer.Serialize(_properties);

    public void AddProperty<T>(T source, SparePropertyFactory<T> factory)
        where T : notnull
    {
        Maybe<AvitoSpareProperty> property = factory(source);
        if (property.HasValue)
            _properties.Add(property.Value.Name, property.Value.Value);
    }

    public void AddProperty<T>(string propertyName, T property)
        where T : notnull
    {
        _properties.Add(propertyName, property);
    }

    public async Task AddProperty<T>(T source, AsyncSparePropertyFactory<T> factory)
        where T : notnull
    {
        Maybe<AvitoSpareProperty> property = await factory(source);
        if (property.HasValue)
            _properties.Add(property.Value.Name, property.Value.Value);
    }

    public Maybe<T> Read<T>(string name)
        where T : notnull
    {
        if (!_properties.TryGetValue(name, out object? value))
            return Maybe<T>.None();
        return Maybe<T>.Some((T)value);
    }
}
