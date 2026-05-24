using Chirp2Ftm400.Mappers;
using Chirp2Ftm400.Models;
using FluentAssertions;

namespace Chirp2Ftm400.Tests;

public sealed class FrequencyMapperTests
{
    private static ChirpChannel Make(decimal freq, string duplex, decimal offset = 0m) =>
        new() { Frequency = freq, Duplex = duplex, Offset = offset, Name = "TEST" };

    [Fact]
    public void Simplex_EmptyDuplex_RxEqualsTx()
    {
        var ch = Make(146.520m, "");
        var (rx, tx, offset, direction, warnings) = FrequencyMapper.Map(ch);

        rx.Should().Be("146.52000");
        tx.Should().Be("146.52000");
        offset.Should().Be("0.00000");
        direction.Should().Be("OFF");
        warnings.Should().BeEmpty();
    }

    [Fact]
    public void PositiveOffset_DirectionIsPlusRPT()
    {
        var ch = Make(146.000m, "+", 0.600m);
        var (rx, tx, offset, direction, warnings) = FrequencyMapper.Map(ch);

        rx.Should().Be("146.00000");
        tx.Should().Be("146.60000");
        offset.Should().Be("0.60000");
        direction.Should().Be("+RPT");
        warnings.Should().BeEmpty();
    }

    [Fact]
    public void NegativeOffset_DirectionIsMinusRPT()
    {
        var ch = Make(146.600m, "-", 0.600m);
        var (rx, tx, offset, direction, warnings) = FrequencyMapper.Map(ch);

        rx.Should().Be("146.60000");
        tx.Should().Be("146.00000");
        offset.Should().Be("0.60000");
        direction.Should().Be("-RPT");
        warnings.Should().BeEmpty();
    }

    [Fact]
    public void SplitDuplex_TxFromOffsetField_OffsetIsAbsDiff()
    {
        // TX (444.000) > RX (146.000) → direction +RPT, offset = 298.000
        var ch = Make(146.000m, "split", 444.000m);
        var (rx, tx, offset, direction, warnings) = FrequencyMapper.Map(ch);

        rx.Should().Be("146.00000");
        tx.Should().Be("444.00000");
        offset.Should().Be("298.00000");
        direction.Should().Be("+RPT");
        warnings.Should().BeEmpty();
    }

    [Fact]
    public void SplitDuplex_TxLessThanRx_DirectionIsMinusRPT()
    {
        var ch = Make(444.000m, "split", 146.000m);
        var (_, _, _, direction, _) = FrequencyMapper.Map(ch);

        direction.Should().Be("-RPT");
    }

    [Fact]
    public void OffDuplex_TxEqualsRx_EmitsWarning()
    {
        var ch = Make(146.520m, "off");
        var (rx, tx, _, direction, warnings) = FrequencyMapper.Map(ch);

        rx.Should().Be("146.52000");
        tx.Should().Be("146.52000");
        direction.Should().Be("OFF");
        warnings.Should().ContainSingle().Which.Should().Contain("TX-disabled");
    }

    [Fact]
    public void FrequencyFormattedToFiveDecimalPlaces()
    {
        var ch = Make(144.0m, "");
        var (rx, tx, _, _, _) = FrequencyMapper.Map(ch);

        rx.Should().Be("144.00000");
        tx.Should().Be("144.00000");
    }
}
