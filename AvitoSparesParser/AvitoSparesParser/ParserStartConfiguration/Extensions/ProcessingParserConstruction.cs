using System.Text;
using System.Text.Json;
using AvitoSparesParser.Common;
using RabbitMQ.Client.Events;

namespace AvitoSparesParser.ParserStartConfiguration.Extensions;

public static class ProcessingParserConstruction
{
    extension(IEnumerable<ProcessingParserLink>)
    {
        public static ProcessingParserLink[] ArrayFromDeliverEventArgs(BasicDeliverEventArgs ea)
        {
            string json = Encoding.UTF8.GetString(ea.Body.Span);
            using JsonDocument document = JsonDocument.Parse(json);

            JsonElement linksElement = document.RootElement.GetProperty("links");
            List<ProcessingParserLink> result = new(linksElement.GetArrayLength());

            foreach (JsonElement jsonLink in linksElement.EnumerateArray())
                result.Add(FromJson(jsonLink));

            return [.. result];
        }

        public static ProcessingParserLink FromJson(JsonElement element)
        {
            Guid id = element.GetProperty("id").GetGuid();
            string url = element.GetProperty("url").GetString()!;

            ProcessingParserLink link = new(
                id,
                url,
                Marker: ProcessedMarker.Unprocessed(),
                Counter: RetryCounter.New());

            return link;
        }
    }

    extension(ProcessingParser)
    {
        public static ProcessingParser FromDeliverEventArgs(BasicDeliverEventArgs ea)
        {
            string json = Encoding.UTF8.GetString(ea.Body.Span);
            using JsonDocument document = JsonDocument.Parse(json);

            Guid id = document.RootElement.GetProperty("parser_id").GetGuid();
            string domain = document.RootElement.GetProperty("parser_domain").GetString()!;
            string type = document.RootElement.GetProperty("parser_type").GetString()!;

            return new ProcessingParser(
                Id: id,
                Domain: domain,
                Type: type,
                Entered: DateTime.UtcNow,
                Finished: null
            );
        }
    }
}