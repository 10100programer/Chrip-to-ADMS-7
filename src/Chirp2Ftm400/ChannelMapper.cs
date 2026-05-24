using Chirp2Ftm400.Mappers;
using Chirp2Ftm400.Models;

namespace Chirp2Ftm400;

public static class ChannelMapper
{
    public static (Ftm400Channel Result, IReadOnlyList<string> Warnings)
        Map(ChirpChannel chirp, int channelNumber, string defaultPower = "HIGH", string defaultStep = "5.0KHz")
    {
        var allWarnings = new List<string>();

        var (rxFreq, txFreq, offset, direction, freqWarnings) = FrequencyMapper.Map(chirp);
        allWarnings.AddRange(freqWarnings);

        var modeResult = ModeMapper.Map(chirp.Mode);
        allWarnings.AddRange(modeResult.Warnings);

        var (toneMode, ctcss, dcs, userCtcss, toneWarnings) = ToneMapper.Map(chirp);
        allWarnings.AddRange(toneWarnings);

        // Use Power column from CHIRP; fall back to default
        var powerResult = PowerMapper.Map(string.IsNullOrWhiteSpace(chirp.Power) ? defaultPower : chirp.Power);
        allWarnings.AddRange(powerResult.Warnings);

        var stepResult = StepMapper.Map(string.IsNullOrWhiteSpace(chirp.TStep) ? defaultStep : chirp.TStep);
        allWarnings.AddRange(stepResult.Warnings);

        var skipResult = SkipMapper.Map(chirp.Skip);
        allWarnings.AddRange(skipResult.Warnings);

        var nameResult = NameMapper.Map(chirp.Name);
        allWarnings.AddRange(nameResult.Warnings);

        var ftmChannel = new Ftm400Channel
        {
            Channel = channelNumber,
            RxFreq = rxFreq,
            TxFreq = txFreq,
            Offset = offset,
            Direction = direction,
            Mode = modeResult.Value,
            Name = nameResult.Value,
            ToneMode = toneMode,
            CTCSS = ctcss,
            DCS = dcs,
            UserCTCSS = userCtcss,
            Power = powerResult.Value,
            Skip = skipResult.Value,
            Step = stepResult.Value,
            MemoryTag = 0,
            Comment = "",
            MemoryTag2 = 0,
        };

        return (ftmChannel, allWarnings);
    }
}
