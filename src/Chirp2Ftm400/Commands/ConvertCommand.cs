using Chirp2Ftm400.Infrastructure;
using Chirp2Ftm400.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Chirp2Ftm400.Commands;

public sealed class ConvertCommand : Command<ConvertSettings>
{
    private readonly IAnsiConsole _console;

    public ConvertCommand(IAnsiConsole console)
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
            _console.Write(new FigletText("chirp2ftm").Color(Color.SteelBlue));
        }

        // Determine output path
        string outputPath = settings.OutputFile
            ?? Path.ChangeExtension(settings.InputFile, ".ftm400.csv");

        if (!settings.Overwrite && File.Exists(outputPath))
        {
            _console.MarkupLine($"[red]Output file already exists:[/] {Markup.Escape(outputPath)}");
            _console.MarkupLine("Use [yellow]--overwrite[/] to overwrite it.");
            return 3;
        }

        if (!File.Exists(settings.InputFile))
        {
            _console.MarkupLine($"[red]Input file not found:[/] {Markup.Escape(settings.InputFile)}");
            return 2;
        }

        // Read CHIRP file
        IReadOnlyList<ChirpChannel> chirpChannels = [];
        IReadOnlyList<string> readWarnings = [];

        _console.Status().Start("Reading CHIRP file...", ctx =>
        {
            ctx.Spinner(Spinner.Known.Dots);
            (chirpChannels, readWarnings) = ChirpCsvReader.Read(settings.InputFile);
        });

        if (!settings.Quiet)
            _console.MarkupLine($"Read [cyan]{chirpChannels.Count}[/] channels from CHIRP file.");

        // Validate
        var (validChannels, droppedChannels) = ChirpValidator.Validate(chirpChannels);
        var outOfBandWarnings = ChirpValidator.GetOutOfBandWarnings(validChannels);

        if (!settings.Quiet && droppedChannels.Count > 0)
        {
            _console.MarkupLine($"[yellow]Dropped {droppedChannels.Count} invalid channel(s).[/]");
        }

        if (validChannels.Count == 0)
        {
            _console.MarkupLine("[red]No valid channels to convert.[/]");
            return 4;
        }

        // Map channels
        var ftmChannels = new List<Ftm400Channel>();
        var mappingWarnings = new List<(int Ch, string Name, string Warning)>();

        _console.Progress()
            .AutoRefresh(!settings.Quiet)
            .HideCompleted(!settings.Quiet)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new ElapsedTimeColumn())
            .Start(ctx =>
            {
                var task = ctx.AddTask("Mapping channels", maxValue: validChannels.Count);

                string defaultStep = settings.DefaultStep.EndsWith("KHz", StringComparison.OrdinalIgnoreCase)
                    ? settings.DefaultStep
                    : settings.DefaultStep + "KHz";

                for (int i = 0; i < validChannels.Count; i++)
                {
                    var chirp = validChannels[i];
                    // CHIRP Location is 0-indexed; FTM-400D channels are 1-indexed
                    int channelNumber = settings.Renumber
                        ? settings.StartChannel + i
                        : chirp.Location + 1;

                    var (ftmChannel, warnings) = ChannelMapper.Map(
                        chirp, channelNumber, settings.DefaultPower, defaultStep);
                    ftmChannels.Add(ftmChannel);

                    foreach (var w in warnings)
                        mappingWarnings.Add((channelNumber, chirp.Name ?? "", w));

                    task.Increment(1);
                }
            });

        // Collect all warnings
        var allWarnings = new List<(int Ch, string Name, string Warning)>();

        foreach (var w in readWarnings)
            allWarnings.Add((0, "", w));

        foreach (var d in droppedChannels)
            allWarnings.Add((d.Location, d.Name, d.Message));

        foreach (var w in outOfBandWarnings)
            allWarnings.Add((w.Location, w.Name, w.Message));

        allWarnings.AddRange(mappingWarnings);

        // Write output
        Ftm400CsvWriter.Write(outputPath, ftmChannels, settings.MaxChannels);

        if (!settings.Quiet)
            _console.MarkupLine($"Wrote [cyan]{outputPath}[/]");

        // Print warning table
        if (allWarnings.Count > 0)
        {
            var warningTable = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn(new TableColumn("[yellow]Ch[/]").Width(6))
                .AddColumn(new TableColumn("[yellow]Name[/]").Width(10))
                .AddColumn(new TableColumn("[yellow]Warning[/]"));

            foreach (var (ch, name, msg) in allWarnings)
            {
                warningTable.AddRow(
                    $"[yellow]⚠[/] {ch}",
                    Markup.Escape(name),
                    Markup.Escape(msg));
            }

            _console.Write(warningTable);
        }

        // Summary table
        if (!settings.Quiet)
        {
            var summaryTable = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold]Conversion Summary[/]")
                .AddColumn("Metric")
                .AddColumn(new TableColumn("Value").Centered());

            summaryTable.AddRow("Input channels", chirpChannels.Count.ToString());
            summaryTable.AddRow("Valid channels", validChannels.Count.ToString());
            summaryTable.AddRow("Output channels", $"{Math.Min(ftmChannels.Count, settings.MaxChannels)} / {settings.MaxChannels}");
            summaryTable.AddRow("Warnings", allWarnings.Count.ToString());

            _console.Write(summaryTable);
        }

        if (allWarnings.Count > 0)
            return settings.Strict ? 1 : 1;  // exit 1 = warnings present
        return 0;
    }
}
