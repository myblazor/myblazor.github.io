using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ObserverMagazine.ContentProcessor;

public static class FrontMatterParser
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>
    /// Splits a markdown file into YAML front matter and markdown body.
    /// Front matter is delimited by --- at the start and end.
    /// </summary>
    public static (FrontMatter FrontMatter, string MarkdownBody) Parse(string rawContent)
    {
        if (!rawContent.StartsWith("---"))
        {
            return (new FrontMatter(), rawContent);
        }

        var endIndex = rawContent.IndexOf("---", 3, StringComparison.Ordinal);
        if (endIndex < 0)
        {
            return (new FrontMatter(), rawContent);
        }

        var yamlBlock = rawContent[3..endIndex].Trim();
        var body = rawContent[(endIndex + 3)..].TrimStart('\r', '\n');

        var frontMatter = Deserializer.Deserialize<FrontMatter>(yamlBlock);
        return (frontMatter, body);
    }

    /// <summary>
    /// Parses an author YAML file into an AuthorProfile.
    /// </summary>
    public static AuthorProfile? ParseAuthor(string yamlContent, string id)
    {
        try
        {
            var profile = Deserializer.Deserialize<AuthorProfile>(yamlContent);
            profile.Id = id;
            return profile;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Derives a slug from a filename like "2026-01-15-welcome-to-observer-magazine".
    /// The full filename (without extension) is used as the slug, including the date
    /// prefix, which guarantees uniqueness across all posts.
    /// </summary>
    public static string DeriveSlug(string fileName) => fileName;

    /// <summary>
    /// Calculates estimated reading time in minutes from markdown text.
    /// Uses 200 words per minute, minimum 1 minute.
    /// </summary>
    public static int CalculateReadingTime(string markdownBody)
    {
        var wordCount = markdownBody
            .Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries)
            .Length;
        return Math.Max(1, (int)Math.Ceiling(wordCount / 200.0));
    }
}

public sealed class FrontMatter
{
    public string Title { get; init; } = "";
    public DateTime Date { get; init; }
    public DateTime? Updated { get; init; }
    public string? Author { get; init; }
    public string? Summary { get; init; }
    public string[]? Tags { get; init; }
    public bool Draft { get; init; }
    public bool Featured { get; init; }
    public string? Series { get; init; }
    public string? Image { get; init; }
}

public sealed class AuthorProfile
{
    public string Id { get; set; } = "";
    public string Name { get; init; } = "";
    public string? Email { get; init; }
    public string? Bio { get; init; }
    public string? Avatar { get; init; }
    public Dictionary<string, string>? Socials { get; init; }
}
