using System.Globalization;
using Chirp2Ftm400.Models;
using CsvHelper;
using CsvHelper.Configuration;

namespace Chirp2Ftm400.Infrastructure;

public static class ChirpCsvReader
{
    public static (IReadOnlyList<ChirpChannel> Channels, IReadOnlyList<string> Warnings)
        Read(string filePath)
    {
        var warnings = new List<string>();
        var channels = new List<ChirpChannel>();

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);

        csv.Context.RegisterClassMap<ChirpChannelMap>();

        try
        {
            channels.AddRange(csv.GetRecords<ChirpChannel>());
        }
        catch (Exception ex)
        {
            warnings.Add($"Error reading CSV: {ex.Message}");
        }

        return (channels, warnings);
    }

    public static (IReadOnlyList<ChirpChannel> Channels, IReadOnlyList<string> Warnings)
        Read(TextReader textReader)
    {
        var warnings = new List<string>();
        var channels = new List<ChirpChannel>();

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
        };

        using var csv = new CsvReader(textReader, config);

        csv.Context.RegisterClassMap<ChirpChannelMap>();

        try
        {
            channels.AddRange(csv.GetRecords<ChirpChannel>());
        }
        catch (Exception ex)
        {
            warnings.Add($"Error reading CSV: {ex.Message}");
        }

        return (channels, warnings);
    }
}

public sealed class ChirpChannelMap : ClassMap<ChirpChannel>
{
    public ChirpChannelMap()
    {
        Map(m => m.Location).Name("Location");
        Map(m => m.Name).Name("Name");
        Map(m => m.Frequency).Name("Frequency");
        Map(m => m.Duplex).Name("Duplex");
        Map(m => m.Offset).Name("Offset");
        Map(m => m.Tone).Name("Tone");
        Map(m => m.rToneFreq).Name("rToneFreq");
        Map(m => m.cToneFreq).Name("cToneFreq");
        Map(m => m.DtcsCode).Name("DtcsCode");
        Map(m => m.DtcsPolarity).Name("DtcsPolarity");
        Map(m => m.RxDtcsCode).Name("RxDtcsCode");
        Map(m => m.CrossMode).Name("CrossMode");
        Map(m => m.Mode).Name("Mode");
        Map(m => m.TStep).Name("TStep");
        Map(m => m.Skip).Name("Skip");
        Map(m => m.Power).Name("Power");
        Map(m => m.Comment).Name("Comment");
        Map(m => m.URCALL).Name("URCALL");
        Map(m => m.RPT1CALL).Name("RPT1CALL");
        Map(m => m.RPT2CALL).Name("RPT2CALL");
        Map(m => m.DVCODE).Name("DVCODE");
    }
}
