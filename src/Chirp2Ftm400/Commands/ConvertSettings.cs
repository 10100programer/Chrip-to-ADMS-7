using System.ComponentModel;
using Spectre.Console.Cli;

namespace Chirp2Ftm400.Commands;

public sealed class ConvertSettings : CommandSettings
{
    [CommandArgument(0, "<input>")]
    [Description("Path to the CHIRP CSV input file")]
    public string InputFile { get; init; } = "";

    [CommandOption("-o|--output")]
    [Description("Path to the FTM-400D ADMS-4 CSV output file")]
    public string? OutputFile { get; init; }

    [CommandOption("--overwrite")]
    [Description("Overwrite the output file if it already exists")]
    public bool Overwrite { get; init; }

    [CommandOption("--max-channels")]
    [Description("Maximum number of channels in the output (default: 900 for ADMS-7)")]
    public int MaxChannels { get; init; } = 900;

    [CommandOption("--renumber")]
    [Description("Renumber channels sequentially instead of using CHIRP Location")]
    public bool Renumber { get; init; }

    [CommandOption("--start-channel")]
    [Description("First channel number when using --renumber (default: 1)")]
    public int StartChannel { get; init; } = 1;

    [CommandOption("--strict")]
    [Description("Treat warnings as errors (exit code 1 becomes non-zero failure)")]
    public bool Strict { get; init; }

    [CommandOption("--default-power")]
    [Description("Power level when CHIRP has none: HIGH, MID, or LOW (default: HIGH)")]
    public string DefaultPower { get; init; } = "HIGH";

    [CommandOption("--default-step")]
    [Description("Step when CHIRP has none, e.g. 5.0 (default: 5.0KHz)")]
    public string DefaultStep { get; init; } = "5.0";

    [CommandOption("-q|--quiet")]
    [Description("Suppress banner and progress output")]
    public bool Quiet { get; init; }

    [CommandOption("-v|--verbose")]
    [Description("Print every channel conversion detail")]
    public bool Verbose { get; init; }
}
