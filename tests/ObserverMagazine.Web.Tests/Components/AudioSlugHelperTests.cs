using ObserverMagazine.Web.Components;
using Xunit;

namespace ObserverMagazine.Web.Tests.Components;

/// <summary>
/// Unit tests for <see cref="AudioSlugHelper"/>.
///
/// These tests verify the two-phase audio resolution strategy used by
/// <see cref="BlogTtsPlayer"/>:
///   1. Try the canonical (date-prefixed) MP3 path.
///   2. If unavailable, fall back to the legacy (dateless) MP3 path.
///
/// The fallback exists because GitHub Actions can hit a full audio cache and
/// skip the legacy-rename migration step, leaving dateless MP3s on GitHub Pages
/// even after the slug scheme was changed to include date prefixes.
/// </summary>
public class AudioSlugHelperTests
{
    // -----------------------------------------------------------------------
    // CanonicalAudioSrc
    // -----------------------------------------------------------------------

    [Fact]
    public void CanonicalAudioSrc_ReturnsDatePrefixedPath()
    {
        var src = AudioSlugHelper.CanonicalAudioSrc(
            "2026-05-04-rust-programming-language-complete-guide");

        Assert.Equal(
            "blog-data/2026-05-04-rust-programming-language-complete-guide.mp3",
            src);
    }

    [Fact]
    public void CanonicalAudioSrc_ReturnsDatelessPath_WhenSlugHasNoDate()
    {
        var src = AudioSlugHelper.CanonicalAudioSrc("welcome-to-observer");

        Assert.Equal("blog-data/welcome-to-observer.mp3", src);
    }

    // -----------------------------------------------------------------------
    // LegacyAudioSrc — posts that have a date prefix (the common case)
    // -----------------------------------------------------------------------

    [Fact]
    public void LegacyAudioSrc_ReturnsDatelessPath_ForDatePrefixedSlug()
    {
        // This is the core scenario: the MP3 on disk is still the old dateless name.
        var src = AudioSlugHelper.LegacyAudioSrc(
            "2026-05-04-rust-programming-language-complete-guide");

        Assert.Equal(
            "blog-data/rust-programming-language-complete-guide.mp3",
            src);
    }

    [Theory]
    [InlineData("2026-01-15-welcome-to-observer-magazine",
                "blog-data/welcome-to-observer-magazine.mp3")]
    [InlineData("2026-03-27-sql-server-complete-guide",
                "blog-data/sql-server-complete-guide.mp3")]
    [InlineData("2026-04-25-javascript",
                "blog-data/javascript.mp3")]
    [InlineData("2099-12-31-future-post",
                "blog-data/future-post.mp3")]
    public void LegacyAudioSrc_StripsPrefixCorrectly(string slug, string expectedSrc)
    {
        Assert.Equal(expectedSrc, AudioSlugHelper.LegacyAudioSrc(slug));
    }

    // -----------------------------------------------------------------------
    // LegacyAudioSrc — posts that have NO date prefix (should return null)
    // -----------------------------------------------------------------------

    [Fact]
    public void LegacyAudioSrc_ReturnsNull_WhenSlugHasNoDatePrefix()
    {
        // A slug with no date prefix has no "older" dateless name to fall back to.
        Assert.Null(AudioSlugHelper.LegacyAudioSrc("welcome-to-observer"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("no-date-at-all")]
    [InlineData("2026-only-two-segments")]          // YYYY-XX only, missing third dash
    [InlineData("abcd-ef-gh-rest")]                 // non-digit year
    [InlineData("2026-AB-01-rest")]                 // non-digit month
    public void LegacyAudioSrc_ReturnsNull_ForNonDatePrefixedSlugs(string slug)
    {
        Assert.Null(AudioSlugHelper.LegacyAudioSrc(slug));
    }

    // -----------------------------------------------------------------------
    // Idempotency: canonical and legacy are always different when a date is present
    // -----------------------------------------------------------------------

    [Fact]
    public void CanonicalAndLegacySrc_AreDifferent_WhenDatePrefixed()
    {
        const string slug = "2026-05-04-rust-programming-language-complete-guide";
        var canonical = AudioSlugHelper.CanonicalAudioSrc(slug);
        var legacy = AudioSlugHelper.LegacyAudioSrc(slug);

        Assert.NotNull(legacy);
        Assert.NotEqual(canonical, legacy);
    }

    // -----------------------------------------------------------------------
    // Edge cases around the YYYY-MM-DD boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void LegacyAudioSrc_HandlesMinimalSlugAfterDate()
    {
        // YYYY-MM-DD-x — the part after the date is just one character
        var src = AudioSlugHelper.LegacyAudioSrc("2026-05-04-x");
        Assert.Equal("blog-data/x.mp3", src);
    }

    [Fact]
    public void LegacyAudioSrc_ReturnsNull_WhenNothingFollowsDate()
    {
        // "2026-05-04-" has length 11 which is NOT > 11, so no slug after the prefix
        Assert.Null(AudioSlugHelper.LegacyAudioSrc("2026-05-04-"));
    }

    [Fact]
    public void LegacyAudioSrc_ReturnsNull_ForExactly11Chars()
    {
        // Exactly 11 chars: "2026-05-04-" but without the trailing "-"
        // e.g. a slug of exactly "2026-05-04" (length 10) — not a valid date-slug pattern
        Assert.Null(AudioSlugHelper.LegacyAudioSrc("2026-05-04"));
    }
}
