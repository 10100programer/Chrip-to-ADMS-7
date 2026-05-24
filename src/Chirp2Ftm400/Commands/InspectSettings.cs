using System.ComponentModel;
using Spectre.Console.Cli;

namespace Chirp2Ftm400.Commands;

public sealed class InspectSettings : CommandSettings
{
    [CommandArgument(0, "<input>")]
    [Description("Path to the CHIRP CSV input file")]
    public string InputFile { get; init; } = "";

    [CommandOption("-o|--output")]
    [Description("(unused for inspect)")]
    public string? OutputFile { get; init; }

    [CommandOption("--overwrite")]
    [Description("(unused for inspect)")]
    public bool Overwrite { get; init; }

    [CommandOption("--max-channels")]
    [Description("Maximum number of channels (default: 500)")]
    public int MaxChannels { get; init; } = 500;

    [CommandOption("--renumber")]
    [Description("Renumber channels sequentially")]
    public bool Renumber { get; init; }

    [CommandOption("--quiet")]
    [Description("Suppress banner")]
    public bool Quiet { get; init; }

    [CommandOption("--page-size")]
    [Description("Number of rows per page (default: 50)")]
    public int PageSize { get; init; } = 50;
}
