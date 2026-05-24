using Chirp2Ftm400.Mappers;
using FluentAssertions;

namespace Chirp2Ftm400.Tests;

public sealed class ModeMapperTests
{
    [Theory]
    [InlineData("FM", "FM")]
    [InlineData("NFM", "NFM")]
    [InlineData("AM", "AM")]
    public void KnownModes_NoWarnings(string input, string expected)
    {
        var result = ModeMapper.Map(input);
        result.Value.Should().Be(expected);
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void WFM_MapsToFM_WithWarning()
    {
        var result = ModeMapper.Map("WFM");
        result.Value.Should().Be("FM");
        result.Warnings.Should().ContainSingle()
            .Which.Should().Contain("WFM mapped to FM");
    }

    [Fact]
    public void DV_MapsToFM_WithWarning()
    {
        // D-Star is not supported on FTM-400D → falls back to FM
        var result = ModeMapper.Map("DV");
        result.Value.Should().Be("FM");
        result.Warnings.Should().ContainSingle()
            .Which.Should().Contain("D-Star");
    }

    [Fact]
    public void DN_MapsToAuto_WithWarning()
    {
        var result = ModeMapper.Map("DN");
        result.Value.Should().Be("AUTO");
        result.Warnings.Should().ContainSingle()
            .Which.Should().Contain("C4FM");
    }

    [Fact]
    public void Unknown_MapsToFM_WithWarning()
    {
        var result = ModeMapper.Map("FOOBAR");
        result.Value.Should().Be("FM");
        result.Warnings.Should().ContainSingle()
            .Which.Should().Contain("Unknown mode");
    }

    [Fact]
    public void Null_MapsToFM_WithWarning()
    {
        var result = ModeMapper.Map(null);
        result.Value.Should().Be("FM");
    }

    [Theory]
    [InlineData("fm")]
    [InlineData("Fm")]
    [InlineData("FM")]
    public void CaseInsensitive_FM(string input)
    {
        var result = ModeMapper.Map(input);
        result.Value.Should().Be("FM");
        result.Warnings.Should().BeEmpty();
    }
}
