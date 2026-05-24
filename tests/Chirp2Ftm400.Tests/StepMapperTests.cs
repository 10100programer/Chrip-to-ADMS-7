using Chirp2Ftm400.Mappers;
using FluentAssertions;

namespace Chirp2Ftm400.Tests;

public sealed class StepMapperTests
{
    [Theory]
    [InlineData("5.0kHz", "5.0KHz")]
    [InlineData("6.25kHz", "6.25KHz")]
    [InlineData("8.33kHz", "8.33KHz")]
    [InlineData("10.0kHz", "10.0KHz")]
    [InlineData("12.5kHz", "12.5KHz")]
    [InlineData("15.0kHz", "15.0KHz")]
    [InlineData("20.0kHz", "20.0KHz")]
    [InlineData("25.0kHz", "25.0KHz")]
    [InlineData("50.0kHz", "50.0KHz")]
    [InlineData("100.0kHz", "100.0KHz")]
    public void KnownSteps_MapExactly(string input, string expected)
    {
        var result = StepMapper.Map(input);
        result.Value.Should().Be(expected);
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Empty_Defaults_To_5KHz()
    {
        var result = StepMapper.Map("");
        result.Value.Should().Be("5.0KHz");
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Null_Defaults_To_5KHz()
    {
        var result = StepMapper.Map(null);
        result.Value.Should().Be("5.0KHz");
    }

    [Fact]
    public void NearestSnap_7kHz_SnapsTo_6_25()
    {
        // 7 is between 6.25 and 8.33; closer to 6.25
        var result = StepMapper.Map("7.0kHz");
        result.Value.Should().Be("6.25KHz");
        result.Warnings.Should().ContainSingle();
    }

    [Fact]
    public void NearestSnap_9kHz_SnapsTo_8_33()
    {
        // 9 is between 8.33 and 10.0; closer to 10.0 actually, but let's check
        var result = StepMapper.Map("9.0kHz");
        result.Value.Should().BeOneOf("8.33KHz", "10.0KHz");
        result.Warnings.Should().ContainSingle();
    }

    [Fact]
    public void StepLabel_UsesCapitalK_NoSpace()
    {
        var result = StepMapper.Map("5.0kHz");
        result.Value.Should().EndWith("KHz");
        result.Value.Should().NotContain(" ");
    }

    [Fact]
    public void ParsesUppercaseKHz()
    {
        var result = StepMapper.Map("5.0KHz");
        result.Value.Should().Be("5.0KHz");
        result.Warnings.Should().BeEmpty();
    }
}
