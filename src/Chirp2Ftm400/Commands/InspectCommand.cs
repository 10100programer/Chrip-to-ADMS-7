using Chirp2Ftm400.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Chirp2Ftm400.Commands;

public sealed class InspectCommand : Command<InspectSettings>
{
    private readonly IAnsiConsole _console;

    public InspectCommand(IAnsiConsole console)
    {
        _console = console;
    }

    protected override int Execute(CommandContext context, InspectSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            return ExecuteInternal(settings);
        }
        catch (Exception ex)
        {
            _console.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return 2;
        }
    }

    private int ExecuteInternal(InspectSettings settings)
    {
        if (!settings.Quiet)
        {
            _console.Write(new FigletText("inspect").Color(Color.SteelBlue));
        }

        if (!File.Exists(settings.InputFile))
        {
            _console.MarkupLine($"[red]Input file not found:[/] {Markup.Escape(settings.InputFile)}");
            return 2;
        }

        var (channels, readWarnings) = ChirpCsvReader.Read(settings.InputFile);

        foreach (var w in readWarnings)
            _console.MarkupLine($"[yellow]Warning:[/] {Markup.Escape(w)}");

        int pageSize = Math.Max(1, settings.PageSize);
        int totalPages = (int)Math.Ceiling(channels.Count / (double)pageSize);

        for (int page = 0; page < totalPages; page++)
        {
            var pageChannels = channels.Skip(page * pageSize).Take(pageSize).ToList();

            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title($"[bold]CHIRP Channels[/] (page {page + 1}/{totalPages})")
                .AddColumn(new TableColumn("[cyan]Loc[/]").Width(5))
                .AddColumn(new TableColumn("[cyan]Name[/]").Width(10))
                .AddColumn(new TableColumn("[cyan]Frequency[/]").Width(12))
                .AddColumn(new TableColumn("[cyan]Duplex[/]").Width(7))
                .AddColumn(new TableColumn("[cyan]Offset[/]").Width(10))
                .AddColumn(new TableColumn("[cyan]Mode[/]").Width(6))
                .AddColumn(new TableColumn("[cyan]Tone[/]").Width(6))
                .AddColumn(new TableColumn("[cyan]Skip[/]").Width(5))
                .AddColumn(new TableColumn("[cyan]Comment[/]"));

            foreach (var ch in pageChannels)
            {
                table.AddRow(
                    ch.Location.ToString(),
                    Markup.Escape(ch.Name ?? ""),
                    ch.Frequency.ToString("F5"),
                    Markup.Escape(ch.Duplex ?? ""),
                    ch.Offset.ToString("F5"),
                    Markup.Escape(ch.Mode ?? ""),
                    Markup.Escape(ch.Tone ?? ""),
                    Markup.Escape(ch.Skip ?? ""),
                    Markup.Escape(ch.Comment ?? ""));
            }

            _console.Write(table);

            if (page < totalPages - 1 && !settings.Quiet)
            {
                _console.MarkupLine("[grey]Press [cyan]Enter[/] for next page...[/]");
                Console.ReadLine();
            }
        }

        _console.MarkupLine($"Total: [cyan]{channels.Count}[/] channels.");
        return 0;
    }
}
