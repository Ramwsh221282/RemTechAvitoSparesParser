namespace AvitoSparesParser.Commands.PrepareAvitoPage;

public interface IPrepareAvitoPageCommand
{
    Task Prepare(Func<string> urlSource);
}