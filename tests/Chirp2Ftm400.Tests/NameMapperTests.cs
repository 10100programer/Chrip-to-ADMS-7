using Chirp2Ftm400.Mappers;
using FluentAssertions;

namespace Chirp2Ftm400.Tests;

public sealed class NameMapperTests
{
    [Fact]
    public void ShortName_Unchanged()
    {
        var result = NameMapper.Map("TEST");
        result.Value.Should().Be("TEST");
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void ExactlyEightChars_Unchanged()
    {
        var result = NameMapper.Map("12345678");
        result.Value.Should().Be("12345678");
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void NineChars_TruncatedToEight_WithWarning()
    {
        var result = NameMapper.Map("123456789");
        result.Value.Should().Be("12345678");
        result.Warnings.Should().ContainSingle()
            .Which.Should().Contain("truncated");
    }

    [Fact]
    public void LongName_TruncatedToEight()
    {
        var result = NameMapper.Map("VeryLongChannelName");
        result.Value.Should().HaveLength(8);
        result.Value.Should().Be("VeryLong");
    }

    [Fact]
    public void NonAsciiChar_ReplacedWithSpace()
    {
        var result = NameMapper.Map("café");
        result.Value.Should().Be("caf ");
        result.Warnings.Should().ContainSingle()
            .Which.Should().Contain("non-ASCII");
    }

    [Fact]
    public void ControlChar_ReplacedWithSpace()
    {
        var result = NameMapper.Map("AB\tCD");
        result.Value.Should().Be("AB CD");
        result.Warnings.Should().ContainSingle();
    }

    [Fact]
    public void Null_ReturnsEmpty_NoWarnings()
    {
        var result = NameMapper.Map(null);
        result.Value.Should().Be("");
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void NonAsciiAndTooLong_BothWarnings()
    {
        // "café123456" → replace é with space → "caf 123456" → truncate to 8 → "caf 1234"
        var result = NameMapper.Map("café123456");
        result.Value.Should().HaveLength(8);
        result.Warnings.Should().HaveCount(2);
    }
}
