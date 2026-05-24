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
            "WFM" => MapResult<string>.Warn("FM", "WFM mapped to FM — ADMS-7 has no broadcast wideband mode"),
            "DN" => MapResult<string>.Warn("FM", "DN (C4FM digital) mapped to FM — ADMS-7 CSV format only supports FM/NFM/AM"),
            "DV" => MapResult<string>.Warn("FM", "DV (D-Star) is not supported; mapped to FM"),
            var other => MapResult<string>.Warn("FM", $"Unknown mode '{other}', defaulting to FM"),
        };
    }
}
