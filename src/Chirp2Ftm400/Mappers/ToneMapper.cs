using Chirp2Ftm400.Models;

namespace Chirp2Ftm400.Mappers;

public static class ToneMapper
{
    private const string DefaultCTCSS = "100.0 Hz";
    private const string DefaultDCS = "023";
    private const string DefaultUserCTCSS = "1500 Hz";

    public static (string ToneMode, string CTCSS, string DCS, string UserCTCSS, IReadOnlyList<string> Warnings)
        Map(ChirpChannel chirp)
    {
        var warnings = new List<string>();
        string tone = (chirp.Tone ?? "").Trim().ToLowerInvariant();

        string toneMode, ctcss, dcs;

        switch (tone)
        {
            case "" or "none":
                toneMode = "OFF";
                // ADMS-4 requires CTCSS and DCS to always be populated; use defaults
                ctcss = DefaultCTCSS;
                dcs = DefaultDCS;
                break;

            case "tone":
                // TX CTCSS encode only → ADMS-4 "TONE ENC"
                toneMode = "TONE ENC";
                ctcss = FormatCtcss(chirp.rToneFreq, warnings);
                dcs = DefaultDCS;
                break;

            case "tsql":
                // TX+RX CTCSS squelch
                toneMode = "T SQL";
                ctcss = FormatCtcss(chirp.cToneFreq, warnings);
                dcs = DefaultDCS;
                break;

            case "dtcs":
                toneMode = "DCS";
                ctcss = DefaultCTCSS;
                dcs = FormatDcs(chirp.DtcsCode);
                break;

            case "cross":
                (toneMode, ctcss, dcs) = MapCrossMode(chirp, warnings);
                break;

            default:
                warnings.Add($"Unknown tone mode '{chirp.Tone}', defaulting to OFF");
                toneMode = "OFF";
                ctcss = DefaultCTCSS;
                dcs = DefaultDCS;
                break;
        }

        return (toneMode, ctcss, dcs, DefaultUserCTCSS, warnings);
    }

    private static string FormatCtcss(string? freq, List<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(freq))
        {
            warnings.Add($"Tone mode requires CTCSS frequency but none set; defaulting to {DefaultCTCSS}");
            return DefaultCTCSS;
        }

        string clean = freq.Replace("Hz", "").Trim();
        if (decimal.TryParse(clean, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal val))
        {
            return $"{val:F1} Hz";
        }

        warnings.Add($"Could not parse CTCSS frequency '{freq}'; defaulting to {DefaultCTCSS}");
        return DefaultCTCSS;
    }

    private static string FormatDcs(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return "023";

        string digits = new string(code.Where(char.IsDigit).ToArray());
        if (int.TryParse(digits, out int n))
            return n.ToString("D3");

        return "023";
    }

    private static (string toneMode, string ctcss, string dcs) MapCrossMode(
        ChirpChannel chirp, List<string> warnings)
    {
        string crossMode = (chirp.CrossMode ?? "").Trim();

        return crossMode.ToUpperInvariant() switch
        {
            // TX+RX tone squelch — use rToneFreq (lossy if cToneFreq differs)
            "TONE->TONE" => ("T SQL", FormatCtcss(chirp.rToneFreq, warnings), DefaultDCS),

            // RX-only tone squelch — use cToneFreq; TX has no tone
            "->TONE" => WarnAndReturn("T SQL", FormatCtcss(chirp.cToneFreq, warnings), DefaultDCS,
                "CrossMode '->Tone': TX has no tone encode; using T SQL with RX freq", warnings),

            // TX tone encode only, RX squelch open
            "TONE->" => ("TONE ENC", FormatCtcss(chirp.rToneFreq, warnings), DefaultDCS),

            "DTCS->DTCS" => ("DCS", DefaultCTCSS, FormatDcs(chirp.DtcsCode)),

            // Mixed: fall back to T SQL with rToneFreq
            "TONE->DTCS" or "DTCS->TONE" => WarnAndReturn("T SQL", FormatCtcss(chirp.rToneFreq, warnings), DefaultDCS,
                $"CrossMode '{crossMode}' mixed CTCSS/DCS — using T SQL with rToneFreq (lossy)", warnings),

            _ => WarnAndReturn("T SQL", FormatCtcss(chirp.rToneFreq, warnings), DefaultDCS,
                $"Complex CrossMode '{crossMode}' not fully supported; using T SQL encode only", warnings),
        };
    }

    private static (string toneMode, string ctcss, string dcs) WarnAndReturn(
        string toneMode, string ctcss, string dcs, string warning, List<string> warnings)
    {
        warnings.Add(warning);
        return (toneMode, ctcss, dcs);
    }
}
