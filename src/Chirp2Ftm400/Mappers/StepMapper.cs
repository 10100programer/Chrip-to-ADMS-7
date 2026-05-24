using System.Globalization;

namespace Chirp2Ftm400.Mappers;

public static class StepMapper
{
    // Valid FTM-400D steps in kHz
    private static readonly (double Khz, string Label)[] ValidSteps =
    [
        (5.0,   "5.0KHz"),
        (6.25,  "6.25KHz"),
        (8.33,  "8.33KHz"),
        (10.0,  "10.0KHz"),
        (12.5,  "12.5KHz"),
        (15.0,  "15.0KHz"),
        (20.0,  "20.0KHz"),
        (25.0,  "25.0KHz"),
        (50.0,  "50.0KHz"),
        (100.0, "100.0KHz"),
    ];

    public static MapResult<string> Map(string? chirpStep)
    {
        if (string.IsNullOrWhiteSpace(chirpStep))
            return MapResult<string>.Ok("5.0KHz");

        // Strip trailing "kHz", "KHz", "KHZ", "khz", then parse
        string stripped = chirpStep.Trim();
        string lower = stripped.ToLowerInvariant();

        if (lower.EndsWith("khz"))
            stripped = stripped[..^3];
        else if (lower.EndsWith("kh"))
            stripped = stripped[..^2];

        if (!double.TryParse(stripped.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double khz))
            return MapResult<string>.Warn("5.0KHz", $"Could not parse step '{chirpStep}', defaulting to 5.0KHz");

        // Find exact match first
        foreach (var (validKhz, label) in ValidSteps)
        {
            if (Math.Abs(validKhz - khz) < 0.001)
                return MapResult<string>.Ok(label);
        }

        // Snap to nearest
        var nearest = ValidSteps.MinBy(s => Math.Abs(s.Khz - khz));
        return MapResult<string>.Warn(nearest.Label,
            $"Step {chirpStep} not supported; snapped to {nearest.Label}");
    }
}
