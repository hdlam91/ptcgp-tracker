using PtcgpTracker.Api.Services;

namespace PtcgpTracker.Api.Tests.Services;

public class ShareHandlesTests
{
    [Theory]
    [InlineData("hd", "hd")]
    [InlineData("Test Trainer", "test-trainer")]
    [InlineData("  Ash   Ketchum!! ", "ash-ketchum")]
    [InlineData("Zoë Müller", "zoe-muller")]
    [InlineData("a_b.c/d", "a-b-c-d")]
    public void Slugify_ProducesLowercaseUrlSafeSlugs(string displayName, string expected)
    {
        Assert.Equal(expected, ShareHandles.Slugify(displayName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("!!!")]
    [InlineData("日本語")]
    public void Slugify_FallsBackWhenNothingSlugSafeRemains(string displayName)
    {
        Assert.Equal("trainer", ShareHandles.Slugify(displayName));
    }

    [Fact]
    public void Slugify_TruncatesLongNamesWithoutATrailingHyphen()
    {
        var slug = ShareHandles.Slugify("aaaaaaaaaaaaaaaaaaaaaaaaaaaaa bbbbbbbbbb");

        Assert.True(slug.Length <= 30);
        Assert.DoesNotContain("--", slug);
        Assert.False(slug.EndsWith('-'));
    }
}
