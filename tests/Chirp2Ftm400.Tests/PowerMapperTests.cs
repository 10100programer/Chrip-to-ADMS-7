using Chirp2Ftm400.Mappers;
using FluentAssertions;

namespace Chirp2Ftm400.Tests;

public sealed class PowerMapperTests
{
    [Theory]
    [InlineData("HIGH")]
    [InlineData("H")]
    [InlineData("50W")]
    [InlineData("25W")]
    public void HighPower_Values(string input)
    {
        var result = PowerMapper.Map(input);
        result.Value.Should().Be("HIGH");
    }

    [Theory]
    [InlineData("MID")]
    [InlineData("M")]
    [InlineData("10W")]
    [InlineData("20W")]
    public void MidPower_Values(string input)
    {
        var result = PowerMapper.Map(input);
        result.Value.Should().Be("MID");
    }

    [Theory]
    [InlineData("LOW")]
    [InlineData("L")]
    [InlineData("5W")]
    public void LowPower_Values(string input)
    {
        var result = PowerMapper.Map(input);
        result.Value.Should().Be("LOW");
    }

    [Fact]
    public void Empty_DefaultsToHigh()
    {
        var result = PowerMapper.Map("");
        result.Value.Should().Be("HIGH");
    }

    [Fact]
    public void Null_DefaultsToHigh()
    {
        var result = PowerMapper.Map(null);
        result.Value.Should().Be("HIGH");
    }

    [Fact]
    public void WattsAbove25_IsHigh()
    {
        var result = PowerMapper.Map("50W");
        result.Value.Should().Be("HIGH");
    }

    [Fact]
    public void WattsBelow10_IsLow()
    {
        var result = PowerMapper.Map("5W");
        result.Value.Should().Be("LOW");
    }
}
