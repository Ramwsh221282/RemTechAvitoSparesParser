using System.Text.Json;

namespace AvitoSparesParser.Common;

public static class PlainJsonStringArrayConstruction
{
    extension(PlainJsonStringArray)
    {
        public static PlainJsonStringArray FromEntries(IEnumerable<string> entries)
        {
            string json = JsonSerializer.Serialize(entries);
            return new PlainJsonStringArray(entries, json);
        }

        public static PlainJsonStringArray FromJson(string json)
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement rootElement = document.RootElement;
            int length = rootElement.GetArrayLength();
            List<string> items = new(length);
            foreach (JsonElement item in rootElement.EnumerateArray())
                items.Add(item.GetString()!);
            return new PlainJsonStringArray(items, json);
        }
    }
}