using Markdig;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Xunit;

namespace ObserverMagazine.Integration.Tests;

public class ContentProcessorTests
{
    [Fact]
    public void FrontMatter_ParsesCorrectly()
    {
        var markdown = """
            ---
            title: Test Post
            date: 2026-03-01
            author: Tester
            summary: A test summary
            tags:
              - test
              - integration
            ---

            ## Hello

            This is the body.
            """;

        var (frontMatter, body) = ParseFrontMatter(markdown);

        Assert.Equal("Test Post", frontMatter.Title);
        Assert.Equal(new DateTime(2026, 3, 1), frontMatter.Date);
        Assert.Equal("Tester", frontMatter.Author);
        Assert.Equal("A test summary", frontMatter.Summary);
        Assert.Equal(["test", "integration"], frontMatter.Tags!);
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
        Assert.Contains("Body content.", body);
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
        return (fm ?? new TestFrontMatter(), body);
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
}

public sealed class TestFrontMatter
{
    public string Title { get; set; } = "";
    public DateTime Date { get; set; }
    public string? Author { get; set; }
    public string? Summary { get; set; }
    public string[]? Tags { get; set; }
}
