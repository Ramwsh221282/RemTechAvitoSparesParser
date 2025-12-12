namespace AvitoSparesParser.Constants;

public static class ServiceConstants
{
    public const string ServiceDomain = "Avito";
    public const string ServiceType = "Запчасти";
    public const string CreateParsersQueue = "create.parsers";
    public const string CreateParsersExchange = "parsers";
    public const string CreateParsersRoutingKey = "parsers.creation";
    public static readonly string CurrentServiceExchange = $"{ServiceDomain}.{ServiceType}";
    public static readonly string CurrentServiceConfirmationQueue = $"{CurrentServiceExchange}.confirmation";
    public static readonly string CurrentServiceStartQueue = $"{CurrentServiceExchange}.start";
}