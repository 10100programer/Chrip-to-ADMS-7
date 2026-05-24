using Chirp2Ftm400.Mappers;
using Chirp2Ftm400.Models;
using FluentAssertions;

namespace Chirp2Ftm400.Tests;

public sealed class ToneMapperTests
{
    private static ChirpChannel MakeChannel(
        string? tone = null,
        string? rToneFreq = null,
        string? cToneFreq = null,
        string? dtcsCode = null,
        string? crossMode = null) =>
        new()
        {
            Frequency = 146.52m,
            Name = "TEST",
            Tone = tone,
            rToneFreq = rToneFreq,
            cToneFreq = cToneFreq,
            DtcsCode = dtcsCode,
            CrossMode = crossMode,
        };

    [Fact]
    public void NoTone_ToneOff_DefaultsPopulated()
    {
        var ch = MakeChannel(tone: "");
        var (toneMode, ctcss, dcs, userCtcss, warnings) = ToneMapper.Map(ch);

        toneMode.Should().Be("OFF");
        // ADMS-7 requires CTCSS and DCS always populated; default CTCSS is 88.5 Hz
        ctcss.Should().Be("88.5 Hz");
        dcs.Should().Be("023");
        warnings.Should().BeEmpty();
    }

    [Fact]
    public void NoneTone_ToneOff()
    {
        var ch = MakeChannel(tone: "none");
        var (toneMode, ctcss, _, _, warnings) = ToneMapper.Map(ch);

        toneMode.Should().Be("OFF");
        warnings.Should().BeEmpty();
    }

    [Fact]
    public void Tone_MapsToneEnc_UsesRToneFreq()
    {
        // CHIRP "Tone" = TX encode only → FTM-400D "TONE ENC"
        var ch = MakeChannel(tone: "Tone", rToneFreq: "100.0");
        var (toneMode, ctcss, dcs, _, _) = ToneMapper.Map(ch);

        toneMode.Should().Be("TONE ENC");
        ctcss.Should().Be("100.0 Hz");
        dcs.Should().Be("023");  // always populated
    }

    [Fact]
    public void TSQL_MapsToToneSQL_UsesCToneFreq()
    {
        var ch = MakeChannel(tone: "TSQL", cToneFreq: "127.3");
        var (toneMode, ctcss, dcs, _, _) = ToneMapper.Map(ch);

        toneMode.Should().Be("TONE SQL");
        ctcss.Should().Be("127.3 Hz");
        dcs.Should().Be("023");  // always populated
    }

    [Fact]
    public void DTCS_UsesDtcsCode_ThreeDigitPadded()
    {
        var ch = MakeChannel(tone: "DTCS", dtcsCode: "23");
        var (toneMode, ctcss, dcs, _, _) = ToneMapper.Map(ch);

        toneMode.Should().Be("DCS");
        ctcss.Should().Be("88.5 Hz");  // always populated with ADMS-7 default
        dcs.Should().Be("023");
    }

    [Fact]
    public void DTCS_AlreadyThreeDigits()
    {
        var ch = MakeChannel(tone: "DTCS", dtcsCode: "156");
        var (toneMode, ctcss, dcs, _, _) = ToneMapper.Map(ch);

        toneMode.Should().Be("DCS");
        ctcss.Should().Be("88.5 Hz");  // always populated with ADMS-7 default
        dcs.Should().Be("156");
    }

    [Fact]
    public void UserCTCSS_AlwaysFixed()
    {
        var ch = MakeChannel(tone: "Tone", rToneFreq: "100.0");
        var (_, _, _, userCtcss, _) = ToneMapper.Map(ch);

        userCtcss.Should().Be("1500 Hz");
    }

    [Fact]
    public void EmptyRToneFreq_DefaultsTo88_5Hz_WithWarning()
    {
        var ch = MakeChannel(tone: "Tone", rToneFreq: null);
        var (toneMode, ctcss, _, _, warnings) = ToneMapper.Map(ch);

        toneMode.Should().Be("TONE ENC");
        ctcss.Should().Be("88.5 Hz");
        warnings.Should().ContainSingle().Which.Should().Contain("defaulting to");
    }

    [Fact]
    public void CrossMode_ToneToTone_Maps_TSQL()
    {
        var ch = MakeChannel(tone: "Cross", crossMode: "Tone->Tone", rToneFreq: "88.5");
        var (toneMode, ctcss, _, _, _) = ToneMapper.Map(ch);

        toneMode.Should().Be("TONE SQL");
        ctcss.Should().Be("88.5 Hz");
    }

    [Fact]
    public void CrossMode_ToneToEmpty_Maps_ToneEnc()
    {
        var ch = MakeChannel(tone: "Cross", crossMode: "Tone->", rToneFreq: "100.0");
        var (toneMode, ctcss, _, _, _) = ToneMapper.Map(ch);

        toneMode.Should().Be("TONE ENC");
        ctcss.Should().Be("100.0 Hz");
    }

    [Fact]
    public void CrossMode_EmptyToTone_Maps_TSQL_WithWarning()
    {
        var ch = MakeChannel(tone: "Cross", crossMode: "->Tone", cToneFreq: "100.0");
        var (toneMode, _, _, _, warnings) = ToneMapper.Map(ch);

        toneMode.Should().Be("TONE SQL");
        warnings.Should().ContainSingle().Which.Should().Contain("TX has no tone");
    }

    [Fact]
    public void CrossMode_DtcsToDtcs_Maps_DCS()
    {
        var ch = MakeChannel(tone: "Cross", crossMode: "DTCS->DTCS", dtcsCode: "25");
        var (toneMode, _, dcs, _, _) = ToneMapper.Map(ch);

        toneMode.Should().Be("DCS");
        dcs.Should().Be("025");
    }
}
