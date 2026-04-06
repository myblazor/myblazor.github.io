using System.Xml.Linq;
using Markdig;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Xunit;

namespace ObserverMagazine.Integration.Tests;

public class ContentProcessorTests
{
    private static readonly XNamespace ContentNs = "http://purl.org/rss/1.0/modules/content/";

    [Fact]
    public void FrontMatter_ParsesAllFields()
    {
        var markdown = """
            ---
            title: Test Post
            date: 2026-03-01
            author: myblazor-team
            summary: A test summary
            tags:
              - test
              - integration
            featured: true
            series: Test Series
            image: /images/test.jpg
            ---
            ## Hello

            This is the body.
            """;

        var (frontMatter, body) = ParseFrontMatter(markdown);

        Assert.Equal("Test Post", frontMatter.Title);
        Assert.Equal(new DateTime(2026, 3, 1), frontMatter.Date);
        Assert.Equal("myblazor-team", frontMatter.Author);
        Assert.Equal("A test summary", frontMatter.Summary);
        Assert.Equal(["test", "integration"], frontMatter.Tags!);
        Assert.True(frontMatter.Featured);
        Assert.Equal("Test Series", frontMatter.Series);
        Assert.Equal("/images/test.jpg", frontMatter.Image);
        Assert.False(frontMatter.Draft);
        Assert.Contains("## Hello", body);
    }

    [Fact]
    public void FrontMatter_HandlesMissingFields()
    {
        var markdown = """
            ---
            title: Minimal Post
            date: 2026-01-01
            ---
            Body content.
            """;

        var (frontMatter, body) = ParseFrontMatter(markdown);

        Assert.Equal("Minimal Post", frontMatter.Title);
        Assert.Null(frontMatter.Author);
        Assert.Null(frontMatter.Tags);
        Assert.False(frontMatter.Featured);
        Assert.False(frontMatter.Draft);
        Assert.Null(frontMatter.Series);
        Assert.Null(frontMatter.Image);
        Assert.Contains("Body content.", body);
    }

    [Fact]
    public void FrontMatter_ParsesDraftField()
    {
        var markdown = """
            ---
            title: Draft Post
            date: 2026-06-01
            draft: true
            ---
            Not ready yet.
            """;

        var (frontMatter, _) = ParseFrontMatter(markdown);

        Assert.True(frontMatter.Draft);
    }

    [Fact]
    public void FrontMatter_ParsesUpdatedDate()
    {
        var markdown = """
            ---
            title: Updated Post
            date: 2026-01-01
            updated: 2026-03-15
            ---
            Body.
            """;

        var (frontMatter, _) = ParseFrontMatter(markdown);

        Assert.Equal(new DateTime(2026, 3, 15), frontMatter.Updated);
    }

    [Fact]
    public void Markdown_ConvertsToHtml()
    {
        var md = "## Hello\n\nThis is **bold** and *italic*.";
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var html = Markdown.ToHtml(md, pipeline);

        Assert.Contains("<h2 id=\"hello\">Hello</h2>", html);
        Assert.Contains("<strong>bold</strong>", html);
        Assert.Contains("<em>italic</em>", html);
    }

    [Fact]
    public void MarkdownTwoWords_ConvertsToHtml()
    {
        var md = "## Hello World\n\nThis is **bold** and *italic*.";
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var html = Markdown.ToHtml(md, pipeline);

        Assert.Contains("<h2 id=\"hello-world\">Hello World</h2>", html);
        Assert.Contains("<strong>bold</strong>", html);
        Assert.Contains("<em>italic</em>", html);
    }

    [Fact]
    public void SlugDerivation_StripsDatePrefix()
    {
        Assert.Equal("welcome-post", DeriveSlug("2026-01-15-welcome-post"));
        Assert.Equal("no-date", DeriveSlug("no-date"));
        Assert.Equal("short", DeriveSlug("short"));
    }

    [Fact]
    public void ReadingTime_CalculatesCorrectly()
    {
        // 200 words = 1 minute, 400 words = 2 minutes
        var shortText = string.Join(" ", Enumerable.Repeat("word", 50));
        Assert.Equal(1, CalculateReadingTime(shortText)); // minimum 1 minute

        var mediumText = string.Join(" ", Enumerable.Repeat("word", 400));
        Assert.Equal(2, CalculateReadingTime(mediumText));

        var longText = string.Join(" ", Enumerable.Repeat("word", 1000));
        Assert.Equal(5, CalculateReadingTime(longText));
    }

    [Fact]
    public void GenerateRss_IncludesBasicElements()
    {
        var posts = new List<RssPostEntry>
        {
            new()
            {
                Slug = "test-post",
                Title = "Test Post",
                Date = new DateTime(2026, 1, 1),
                Summary = "A test post",
                Tags = ["test"]
            }
        };

        var xml = GenerateRss("Test Blog", "A test blog", "https://example.com", posts);
        var doc = XDocument.Parse(xml);
        var channel = doc.Root!.Element("channel")!;

        Assert.Equal("Test Blog", channel.Element("title")!.Value);
        Assert.Equal("https://example.com", channel.Element("link")!.Value);

        var item = channel.Element("item")!;
        Assert.Equal("Test Post", item.Element("title")!.Value);
        Assert.Equal("https://example.com/blog/test-post", item.Element("link")!.Value);
    }

    [Fact]
    public void GenerateRss_HandlesEmptyPostList()
    {
        var xml = GenerateRss("Empty Blog", "Nothing here", "https://example.com", []);
        var doc = XDocument.Parse(xml);
        var items = doc.Root!.Element("channel")!.Elements("item");
        Assert.Empty(items);
    }

    [Fact]
    public void GenerateRss_IncludesCategoryTags()
    {
        var posts = new List<RssPostEntry>
        {
            new()
            {
                Slug = "tagged",
                Title = "Tagged Post",
                Date = new DateTime(2026, 1, 1),
                Summary = "Has tags",
                Tags = ["alpha", "beta"]
            }
        };

        var xml = GenerateRss("Blog", "Desc", "https://example.com", posts);
        var doc = XDocument.Parse(xml);
        var categories = doc.Root!.Element("channel")!
            .Element("item")!.Elements("category").Select(c => c.Value).ToArray();

        Assert.Equal(["alpha", "beta"], categories);
    }

    [Fact]
    public void GenerateRss_IncludesFullContentWhenProvided()
    {
        var posts = new List<RssPostEntry>
        {
            new()
            {
                Slug = "full-content",
                Title = "Full Content Post",
                Date = new DateTime(2026, 2, 1),
                Summary = "Has full content",
                Tags = []
            }
        };

        var htmlMap = new Dictionary<string, string>
        {
            ["full-content"] = "<p>This is the <strong>full</strong> content.</p>"
        };

        var xml = GenerateRss("Blog", "Desc", "https://example.com", posts,
            slug => htmlMap.GetValueOrDefault(slug));

        var doc = XDocument.Parse(xml);
        var item = doc.Root!.Element("channel")!.Element("item")!;
        var encoded = item.Element(ContentNs + "encoded");

        Assert.NotNull(encoded);
        Assert.Contains("<strong>full</strong>", encoded.Value);
    }

    [Fact]
    public void GenerateRss_IncludesAuthorWhenEmailProvided()
    {
        var posts = new List<RssPostEntry>
        {
            new()
            {
                Slug = "authored",
                Title = "Authored Post",
                Date = new DateTime(2026, 1, 1),
                Summary = "Has author",
                Tags = [],
                AuthorName = "Observer Team",
                AuthorEmail = "hello@observermagazine.example"
            }
        };

        var xml = GenerateRss("Blog", "Desc", "https://example.com", posts);
        var doc = XDocument.Parse(xml);
        var author = doc.Root!.Element("channel")!.Element("item")!.Element("author");

        Assert.NotNull(author);
        Assert.Contains("hello@observermagazine.example", author.Value);
        Assert.Contains("Observer Team", author.Value);
    }

    // --- Helpers duplicated from ContentProcessor for isolated testing ---

    private static (TestFrontMatter, string) ParseFrontMatter(string rawContent)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        if (!rawContent.StartsWith("---"))
            return (new TestFrontMatter(), rawContent);

        var endIndex = rawContent.IndexOf("---", 3, StringComparison.Ordinal);
        if (endIndex < 0)
            return (new TestFrontMatter(), rawContent);

        var yaml = rawContent[3..endIndex].Trim();
        var body = rawContent[(endIndex + 3)..].TrimStart('\r', '\n');
        var fm = deserializer.Deserialize<TestFrontMatter>(yaml);
        return (fm, body);
    }

    private static string DeriveSlug(string fileName)
    {
        if (fileName.Length > 11 &&
            char.IsDigit(fileName[0]) &&
            fileName[4] == '-' &&
            fileName[7] == '-' &&
            fileName[10] == '-')
        {
            return fileName[11..];
        }
        return fileName;
    }

    private static int CalculateReadingTime(string markdownBody)
    {
        var wordCount = markdownBody
            .Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries)
            .Length;
        return Math.Max(1, (int)Math.Ceiling(wordCount / 200.0));
    }

    private static string GenerateRss(
        string title, string description, string siteUrl,
        IReadOnlyList<RssPostEntry> posts,
        Func<string, string?>? getPostHtml = null)
    {
        var items = posts.Select(p =>
        {
            var itemElements = new List<object>
            {
                new XElement("title", p.Title),
                new XElement("link", $"{siteUrl}/blog/{p.Slug}"),
                new XElement("description", p.Summary),
                new XElement("pubDate", p.Date.ToString("R")),
                new XElement("guid", $"{siteUrl}/blog/{p.Slug}")
            };

            if (!string.IsNullOrEmpty(p.AuthorEmail))
            {
                itemElements.Add(new XElement("author", $"{p.AuthorEmail} ({p.AuthorName})"));
            }

            var html = getPostHtml?.Invoke(p.Slug);
            if (!string.IsNullOrEmpty(html))
            {
                itemElements.Add(new XElement(ContentNs + "encoded", new XCData(html)));
            }

            if (p.Tags.Length > 0)
            {
                itemElements.AddRange(p.Tags.Select(t => new XElement("category", t)));
            }

            return new XElement("item", itemElements);
        });

        var rss = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("rss",
                new XAttribute("version", "2.0"),
                new XAttribute(XNamespace.Xmlns + "content", ContentNs),
                new XElement("channel",
                    new XElement("title", title),
                    new XElement("link", siteUrl),
                    new XElement("description", description),
                    new XElement("language", "en-us"),
                    new XElement("lastBuildDate", DateTime.UtcNow.ToString("R")),
                    items
                )
            )
        );

        return rss.Declaration + Environment.NewLine + rss;
    }
}

public sealed class TestFrontMatter
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
