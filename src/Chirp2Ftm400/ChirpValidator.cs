using Chirp2Ftm400.Models;

namespace Chirp2Ftm400;

public static class ChirpValidator
{
    private static readonly HashSet<string> ValidDuplexValues =
        new(StringComparer.OrdinalIgnoreCase) { "", "+", "-", "split", "off" };

    // Known amateur radio band ranges (MHz)
    private static readonly (decimal Min, decimal Max)[] HamBands =
    [
        (1.800m,   2.000m),   // 160m
        (3.500m,   4.000m),   // 80m
        (5.330m,   5.405m),   // 60m
        (7.000m,   7.300m),   // 40m
        (10.100m,  10.150m),  // 30m
        (14.000m,  14.350m),  // 20m
        (18.068m,  18.168m),  // 17m
        (21.000m,  21.450m),  // 15m
        (24.890m,  24.990m),  // 12m
        (28.000m,  29.700m),  // 10m
        (50.000m,  54.000m),  // 6m
        (144.000m, 148.000m), // 2m
        (220.000m, 225.000m), // 1.25m
        (420.000m, 450.000m), // 70cm
        (902.000m, 928.000m), // 33cm
        (1240.000m,1300.000m),// 23cm
    ];

    public static (IReadOnlyList<ChirpChannel> Valid, IReadOnlyList<ValidationWarning> Dropped)
        Validate(IReadOnlyList<ChirpChannel> channels)
    {
        var valid = new List<ChirpChannel>();
        var dropped = new List<ValidationWarning>();

        foreach (var ch in channels)
        {
            string name = ch.Name ?? "";

            // V1: Frequency must be > 0
            if (ch.Frequency <= 0)
            {
                dropped.Add(new ValidationWarning(ch.Location, name,
                    $"V1: Frequency must be > 0 (got {ch.Frequency})"));
                continue;
            }

            // V2: Location must be >= 0
            if (ch.Location < 0)
            {
                dropped.Add(new ValidationWarning(ch.Location, name,
                    "V2: Location must be >= 0"));
                continue;
            }

            // V3: Name must not be null (can be empty)
            if (ch.Name is null)
            {
                dropped.Add(new ValidationWarning(ch.Location, name,
                    "V3: Name must not be null"));
                continue;
            }

            // V4: Duplex must be a recognised value
            string duplex = (ch.Duplex ?? "").Trim();
            if (!ValidDuplexValues.Contains(duplex))
            {
                dropped.Add(new ValidationWarning(ch.Location, name,
                    $"V4: Unknown duplex value '{ch.Duplex}'; channel skipped"));
                continue;
            }

            // V5: Warn if not in a ham band (keep the channel)
            if (!IsInHamBand(ch.Frequency))
            {
                // Add warning but don't drop — the ValidationWarning here acts as a
                // non-fatal advisory. We still add to valid.
                // We'll carry it as a "dropped" with a warning prefix so callers can display it.
                // Per spec: "warn but keep". We use a separate list approach:
                // since Dropped means "dropped", we add to valid and return the warning separately.
                // Represent as an informational dropped entry with a flag. For simplicity per spec
                // we add to valid and add an out-of-band remark.
                // Actually, re-reading spec: "warn but keep" so we keep it in valid.
                // We'll just add it to valid; commands can emit the V5 warning if desired.
            }

            valid.Add(ch);
        }

        return (valid, dropped);
    }

    public static IReadOnlyList<ValidationWarning> GetOutOfBandWarnings(IReadOnlyList<ChirpChannel> channels)
    {
        var warnings = new List<ValidationWarning>();
        foreach (var ch in channels)
        {
            if (ch.Frequency > 0 && !IsInHamBand(ch.Frequency))
            {
                warnings.Add(new ValidationWarning(ch.Location, ch.Name ?? "",
                    $"V5: Frequency {ch.Frequency} MHz is not in a standard ham band"));
            }
        }
        return warnings;
    }

    private static bool IsInHamBand(decimal freq)
    {
        foreach (var (min, max) in HamBands)
        {
            if (freq >= min && freq <= max)
                return true;
        }
        return false;
    }
}
