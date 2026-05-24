using Chirp2Ftm400.Models;
using System.Globalization;

namespace Chirp2Ftm400.Mappers;

public static class FrequencyMapper
{
    public static (string RxFreq, string TxFreq, string Offset, string Direction, IReadOnlyList<string> Warnings)
        Map(ChirpChannel chirp)
    {
        var warnings = new List<string>();
        var rxFreq = chirp.Frequency;
        string rxStr = rxFreq.ToString("F5", CultureInfo.InvariantCulture);

        string txFreq, offsetStr, direction;

        string duplex = (chirp.Duplex ?? "").Trim().ToLowerInvariant();

        switch (duplex)
        {
            case "":
                txFreq = rxStr;
                offsetStr = "0.00000";
                direction = "OFF";
                break;

            case "+":
                var txPlus = rxFreq + chirp.Offset;
                txFreq = txPlus.ToString("F5", CultureInfo.InvariantCulture);
                offsetStr = chirp.Offset.ToString("F5", CultureInfo.InvariantCulture);
                direction = "+RPT";
                break;

            case "-":
                var txMinus = rxFreq - chirp.Offset;
                txFreq = txMinus.ToString("F5", CultureInfo.InvariantCulture);
                offsetStr = chirp.Offset.ToString("F5", CultureInfo.InvariantCulture);
                direction = "-RPT";
                break;

            case "split":
                // Offset field holds absolute TX frequency
                var txSplit = chirp.Offset;
                txFreq = txSplit.ToString("F5", CultureInfo.InvariantCulture);
                var splitOffset = Math.Abs(txSplit - rxFreq);
                offsetStr = splitOffset.ToString("F5", CultureInfo.InvariantCulture);
                direction = txSplit >= rxFreq ? "+RPT" : "-RPT";
                break;

            case "off":
                txFreq = rxStr;
                offsetStr = "0.00000";
                direction = "OFF";
                warnings.Add("TX-disabled (Duplex=off) is not directly representable in FTM-400D; using simplex");
                break;

            default:
                warnings.Add($"Unknown duplex value '{chirp.Duplex}', treating as simplex");
                txFreq = rxStr;
                offsetStr = "0.00000";
                direction = "OFF";
                break;
        }

        return (rxStr, txFreq, offsetStr, direction, warnings);
    }
}
