using System.Globalization;
using System.Text;
using Chirp2Ftm400.Models;

namespace Chirp2Ftm400.Infrastructure;

public static class Ftm400CsvWriter
{
    public static void Write(
        string filePath,
        IReadOnlyList<Ftm400Channel> channels,
        int maxChannels = 500)
    {
        using var writer = new StreamWriter(filePath, append: false, encoding: new UTF8Encoding(false));
        Write(writer, channels, maxChannels);
    }

    public static void Write(
        TextWriter writer,
        IReadOnlyList<Ftm400Channel> channels,
        int maxChannels = 500)
    {
        // Build a lookup by channel number
        var byChannel = channels.ToDictionary(c => c.Channel);

        for (int i = 1; i <= maxChannels; i++)
        {
            if (byChannel.TryGetValue(i, out var ch))
            {
                writer.WriteLine(FormatChannel(ch));
            }
            else
            {
                writer.WriteLine(FormatEmptyChannel(i));
            }
        }
    }

    private static string FormatChannel(Ftm400Channel ch)
    {
        // Channel,RxFreq,TxFreq,Offset,Direction,Mode,Name,ToneMode,CTCSS,DCS,UserCTCSS,Power,Skip,Step,MemoryTag,Comment,MemoryTag2
        return string.Create(CultureInfo.InvariantCulture,
            $"{ch.Channel},{ch.RxFreq},{ch.TxFreq},{ch.Offset},{ch.Direction},{ch.Mode},{ch.Name},{ch.ToneMode},{ch.CTCSS},{ch.DCS},{ch.UserCTCSS},{ch.Power},{ch.Skip},{ch.Step},{ch.MemoryTag},{ch.Comment},{ch.MemoryTag2}");
    }

    private static string FormatEmptyChannel(int n)
    {
        return $"{n},,,,,,,,,,,,,,0,,0";
    }
}
