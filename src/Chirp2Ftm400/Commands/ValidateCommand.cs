using Chirp2Ftm400.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Chirp2Ftm400.Commands;

public sealed class ValidateCommand : Command<ConvertSettings>
{
    private readonly IAnsiConsole _console;

    public ValidateCommand(IAnsiConsole console)
    {
        _console = console;
    }

    protected override int Execute(CommandContext context, ConvertSettings settings, CancellationToken cancellationToken)
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

    private int ExecuteInternal(ConvertSettings settings)
    {
        if (!settings.Quiet)
        {
            _console.Write(new FigletText("validate").Color(Color.SteelBlue));
        }

        if (!File.Exists(settings.InputFile))
        {
            _console.MarkupLine($"[red]Input file not found:[/] {Markup.Escape(settings.InputFile)}");
            return 2;
        }

        // Read
        var (chirpChannels, readWarnings) = ChirpCsvReader.Read(settings.InputFile);
        _console.MarkupLine($"Read [cyan]{chirpChannels.Count}[/] channels.");

        // Validate
        var (validChannels, droppedChannels) = ChirpValidator.Validate(chirpChannels);
        var outOfBandWarnings = ChirpValidator.GetOutOfBandWarnings(validChannels);

        // Print dropped channels
        if (droppedChannels.Count > 0)
        {
            var droppedTable = new Table()
                .Border(TableBorder.Rounded)
                .Title("[red]Dropped Channels[/]")
                .AddColumn("Location")
                .AddColumn("Name")
                .AddColumn("Reason");

            foreach (var d in droppedChannels)
                droppedTable.AddRow(d.Location.ToString(), Markup.Escape(d.Name), Markup.Escape(d.Message));

            _console.Write(droppedTable);
        }

        // Print out-of-band warnings
        if (outOfBandWarnings.Count > 0)
        {
            var oobTable = new Table()
                .Border(TableBorder.Rounded)
                .Title("[yellow]Out-of-Band Warnings[/]")
                .AddColumn("Location")
                .AddColumn("Name")
                .AddColumn("Warning");

            foreach (var w in outOfBandWarnings)
                oobTable.AddRow(w.Location.ToString(), Markup.Escape(w.Name), Markup.Escape(w.Message));

            _console.Write(oobTable);
        }

        // Summary
        var summaryTable = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]Validation Summary[/]")
            .AddColumn("Metric")
            .AddColumn(new TableColumn("Value").Centered());

        summaryTable.AddRow("Total channels", chirpChannels.Count.ToString());
        summaryTable.AddRow("Valid channels", validChannels.Count.ToString());
        summaryTable.AddRow("Dropped channels", droppedChannels.Count.ToString());
        summaryTable.AddRow("Out-of-band warnings", outOfBandWarnings.Count.ToString());
        summaryTable.AddRow("Read warnings", readWarnings.Count.ToString());

        _console.Write(summaryTable);

        bool hasIssues = droppedChannels.Count > 0 || readWarnings.Count > 0;
        return hasIssues ? 1 : 0;
    }
}
