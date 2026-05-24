namespace Chirp2Ftm400.Mappers;

public static class SkipMapper
{
    public static MapResult<string> Map(string? chirpSkip)
    {
        return (chirpSkip ?? "").Trim().ToUpperInvariant() switch
        {
            "" => MapResult<string>.Ok("OFF"),
            "S" => MapResult<string>.Ok("SKIP"),
            "P" => MapResult<string>.Ok("SELECT"),
            var other => MapResult<string>.Warn("OFF", $"Unknown skip value '{other}'; defaulting to OFF"),
        };
    }
}
