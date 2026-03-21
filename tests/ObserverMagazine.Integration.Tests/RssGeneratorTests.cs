using System.Xml.Linq;
using Xunit;

namespace ObserverMagazine.Integration.Tests;

public class RssGeneratorTests
{
    private static readonly XNamespace ContentNs = "http://purl.org/rss/1.0/modules/content/";

    [Fact]
    public void GenerateRss_ProducesValidXml()
    {
        var posts = new List<RssPostEntry>
        {
            new()
            {
                Slug = "test-post",
                Title = "Test Post",
                Date = new DateTime(2026, 3, 1),
                Summary = "A test post",
                Tags = ["test"]
            }
        };

        var xml = GenerateRss("Test Blog", "A blog", "https://example.com", posts);

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

    // --- RSS generation logic (mirrors ContentProcessor) ---
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

public sealed class RssPostEntry
{
    public string Slug { get; init; } = "";
    public string Title { get; init; } = "";
    public DateTime Date { get; init; }
    public string Summary { get; init; } = "";
    public string[] Tags { get; init; } = [];
    public string AuthorName { get; init; } = "";
    public string? AuthorEmail { get; init; }
}

