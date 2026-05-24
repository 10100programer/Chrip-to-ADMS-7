using Chirp2Ftm400.Mappers;
using FluentAssertions;

namespace Chirp2Ftm400.Tests;

public sealed class SkipMapperTests
{
    [Fact]
    public void Empty_MapsToOff()
    {
        var result = SkipMapper.Map("");
        result.Value.Should().Be("OFF");
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Null_MapsToOff()
    {
        var result = SkipMapper.Map(null);
        result.Value.Should().Be("OFF");
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void S_MapsToSkip()
    {
        var result = SkipMapper.Map("S");
        result.Value.Should().Be("SKIP");
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void P_MapsToScan_NoWarning()
    {
        var result = SkipMapper.Map("P");
        result.Value.Should().Be("SCAN");
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void LowerCase_S_MapsToSkip()
    {
        var result = SkipMapper.Map("s");
        result.Value.Should().Be("SKIP");
    }
}
