namespace Chirp2Ftm400.Mappers;

public static class ModeMapper
{
    public static MapResult<string> Map(string? chirpMode)
    {
        return (chirpMode ?? "").Trim().ToUpperInvariant() switch
        {
            "FM" => MapResult<string>.Ok("FM"),
            "NFM" => MapResult<string>.Ok("NFM"),
            "AM" => MapResult<string>.Ok("AM"),
            "WFM" => MapResult<string>.Warn("FM", "WFM mapped to FM — FTM-400D has no broadcast wideband mode"),
            "DN" => MapResult<string>.Warn("AUTO", "DN (C4FM digital) mapped to AUTO — radio will fall back to analog if needed"),
            "DV" => MapResult<string>.Warn("FM", "DV (D-Star) is not supported on FTM-400D; mapped to FM"),
            var other => MapResult<string>.Warn("FM", $"Unknown mode '{other}', defaulting to FM"),
        };
    }
}
