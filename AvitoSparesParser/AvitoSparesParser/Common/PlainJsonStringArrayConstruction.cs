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
    }
}