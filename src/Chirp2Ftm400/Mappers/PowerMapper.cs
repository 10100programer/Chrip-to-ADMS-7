namespace Chirp2Ftm400.Mappers;

public static class PowerMapper
{
    public static MapResult<string> Map(string? chirpPower)
    {
        if (string.IsNullOrWhiteSpace(chirpPower))
            return MapResult<string>.Ok("HIGH");

        string upper = chirpPower.ToUpperInvariant();

        // Check for numeric wattage patterns
        if (TryExtractWatts(upper, out int watts))
        {
            if (watts >= 25) return MapResult<string>.Ok("HIGH");
            if (watts >= 10) return MapResult<string>.Ok("MID");
            return MapResult<string>.Ok("LOW");
        }

        // Letter-based shortcuts
        if (upper.Contains('H') || upper.Contains("50W") || upper.Contains("25W"))
            return MapResult<string>.Ok("HIGH");

        if (upper.Contains('M') || upper.Contains("10W") || upper.Contains("20W"))
            return MapResult<string>.Ok("MID");

        if (upper.Contains('L') || upper.Contains("5W"))
            return MapResult<string>.Ok("LOW");

        return MapResult<string>.Ok("HIGH");
    }

    private static bool TryExtractWatts(string upper, out int watts)
    {
        watts = 0;
        // e.g. "50W", "5W", "25W"
        int idx = upper.IndexOf('W');
        if (idx <= 0) return false;

        string numPart = upper[..idx].Trim();
        return int.TryParse(numPart, out watts);
    }
}
