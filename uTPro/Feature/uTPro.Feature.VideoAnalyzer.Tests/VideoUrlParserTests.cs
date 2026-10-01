using uTPro.Feature.VideoAnalyzer.Providers;
using uTPro.Feature.VideoAnalyzer.Services;
using Xunit;

namespace uTPro.Feature.VideoAnalyzer.Tests;

/// <summary>Coverage for the URL parser — the entry point every analyze request goes through.</summary>
public sealed class VideoUrlParserTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("http://youtube.com/watch?app=desktop&v=abcdefghijk", "abcdefghijk")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/live/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://m.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    public void Parse_Extracts_VideoId(string input, string expectedId)
    {
        var result = VideoUrlParseResult.Parse(input);
        Assert.True(result.Success, result.Error);
        Assert.Equal("youtube", result.Platform);
        Assert.Equal(expectedId, result.VideoId);
    }

    [Fact]
    public void Parse_Watch_With_List_Param_Flags_Playlist()
    {
        var result = VideoUrlParseResult.Parse("https://www.youtube.com/watch?v=dQw4w9WgXcQ&list=PL1234567890");
        Assert.True(result.Success);
        Assert.Equal("dQw4w9WgXcQ", result.VideoId);
        Assert.True(result.IsPlaylist);
    }

    [Theory]
    [InlineData("https://www.youtube.com/playlist?list=PL1234567890")]
    [InlineData("https://www.tiktok.com/@user/video/1234567890")]
    [InlineData("https://vimeo.com/123456789")]
    [InlineData("not a url at all")]
    [InlineData("")]
    [InlineData(null)]
    public void Parse_Rejects_Unsupported_Input(string? input)
    {
        var result = VideoUrlParseResult.Parse(input);
        Assert.False(result.Success);
        Assert.False(string.IsNullOrEmpty(result.Error));
    }

    [Fact]
    public void Parse_TikTok_Reports_TikTok_Platform()
    {
        var result = VideoUrlParseResult.Parse("https://www.tiktok.com/@user/video/1234567890");
        Assert.False(result.Success);
        Assert.Equal("tiktok", result.Platform);
    }
}

public sealed class IsoDurationTests
{
    [Theory]
    [InlineData("PT4M13S", 253)]
    [InlineData("PT1H2M10S", 3730)]
    [InlineData("PT45S", 45)]
    [InlineData("PT2H", 7200)]
    [InlineData("", 0)]
    [InlineData("garbage", 0)]
    public void ParseIsoDuration_Parses_YouTube_Durations(string iso, int expectedSeconds)
    {
        Assert.Equal(expectedSeconds, YouTubeDataApiProvider.ParseIsoDuration(iso));
    }
}
