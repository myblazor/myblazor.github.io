namespace ObserverMagazine.Web.Components;

/// <summary>
/// Helpers for resolving audio file paths, including backward-compatibility
/// with the pre-migration era when MP3 filenames had no date prefix.
/// </summary>
public static class AudioSlugHelper
{
    /// <summary>
    /// Returns the legacy (dateless) audio src for a slug that carries a
    /// <c>YYYY-MM-DD-</c> prefix, or <c>null</c> when no date prefix is present
    /// (meaning there is no older filename to fall back to).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Before the slug migration, MP3 files were named after the post topic only,
    /// e.g. <c>rust-programming-language-complete-guide.mp3</c>.  After the
    /// migration <c>derive_slug</c> in <c>generate_audio.py</c> was changed to
    /// include the date prefix, producing
    /// <c>2026-05-04-rust-programming-language-complete-guide.mp3</c>.
    /// </para>
    /// <para>
    /// When a GitHub Actions cache hit causes the migration step to be skipped,
    /// the old dateless file may still be the only copy on disk.  This helper
    /// lets <see cref="BlogTtsPlayer"/> try the dateless path as a fallback before
    /// giving up and hiding the player.
    /// </para>
    /// <para>
    /// The date prefix is validated structurally: the slug must be longer than
    /// <c>YYYY-MM-DD-</c> (11 chars), the three dashes must be in the expected
    /// positions, and the year, month, and day groups must all be ASCII digits.
    /// Calendar validity (e.g. month &lt;= 12, day &lt;= 31) is intentionally not
    /// enforced — this is a filename-shape check, not a date parser.
    /// </para>
    /// </remarks>
    /// <param name="slug">The full date-prefixed slug, e.g.
    ///   <c>2026-05-04-rust-programming-language-complete-guide</c>.</param>
    /// <returns>
    /// A relative <c>blog-data/{datelessSlug}.mp3</c> URL, or <c>null</c>.
    /// </returns>
    public static string? LegacyAudioSrc(string slug)
    {
        // Pattern: YYYY-MM-DD-rest-of-slug  (min length = 11 chars for prefix + 1 for rest)
        if (slug.Length > 11
            && slug[4] == '-'
            && slug[7] == '-'
            && slug[10] == '-'
            && slug[..4].All(char.IsAsciiDigit)   // year
            && slug[5..7].All(char.IsAsciiDigit)  // month
            && slug[8..10].All(char.IsAsciiDigit)) // day
        {
            var datelessSlug = slug[11..];
            return $"blog-data/{datelessSlug}.mp3";
        }

        return null;
    }

    /// <summary>
    /// Returns the canonical (date-prefixed) audio src for a given slug.
    /// </summary>
    public static string CanonicalAudioSrc(string slug) => $"blog-data/{slug}.mp3";
}
