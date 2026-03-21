using System.Text.Json;
using ObserverMagazine.ContentProcessor;

// Parse command-line arguments
string contentDir = "content/blog";
string outputDir = "src/ObserverMagazine.Web/wwwroot";

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--content-dir" && i + 1 < args.Length)
        contentDir = args[++i];
    else if (args[i] == "--output-dir" && i + 1 < args.Length)
        outputDir = args[++i];
}

Console.WriteLine($"Content directory: {contentDir}");
Console.WriteLine($"Output directory:  {outputDir}");

if (!Directory.Exists(contentDir))
{
    Console.WriteLine($"Content directory '{contentDir}' does not exist. Creating with no posts.");
    Directory.CreateDirectory(contentDir);
}

string blogDataDir = Path.Combine(outputDir, "blog-data");
Directory.CreateDirectory(blogDataDir);

// Process markdown files
var markdownFiles = Directory.GetFiles(contentDir, "*.md", SearchOption.TopDirectoryOnly);
Console.WriteLine($"Found {markdownFiles.Length} markdown files");

var allPostMetadata = new List<PostIndexEntry>();
var postHtmlMap = new Dictionary<string, string>();

foreach (var mdFile in markdownFiles)
{
    Console.WriteLine($"Processing: {Path.GetFileName(mdFile)}");

    var rawContent = File.ReadAllText(mdFile);
    var (frontMatter, markdownBody) = FrontMatterParser.Parse(rawContent);

    if (string.IsNullOrEmpty(frontMatter.Title))
    {
        Console.WriteLine($"  WARNING: No title in front matter, skipping {mdFile}");
        continue;
    }

    // Derive slug from filename: "2026-01-15-welcome-to-observer-magazine.md" -> "welcome-to-observer-magazine"
    var fileName = Path.GetFileNameWithoutExtension(mdFile);
    var slug = FrontMatterParser.DeriveSlug(fileName);

    var html = MarkdownProcessor.ToHtml(markdownBody);

    // Write individual post HTML
    var htmlPath = Path.Combine(blogDataDir, $"{slug}.html");
    File.WriteAllText(htmlPath, html);
    Console.WriteLine($"  Wrote: {htmlPath}");

    postHtmlMap[slug] = html;

    allPostMetadata.Add(new PostIndexEntry
    {
        Slug = slug,
        Title = frontMatter.Title,
        Date = frontMatter.Date,
        Author = frontMatter.Author ?? "",
        Summary = frontMatter.Summary ?? "",
        Tags = frontMatter.Tags ?? []
    });
}

// Sort by date descending
allPostMetadata.Sort((a, b) => b.Date.CompareTo(a.Date));

// Write posts index JSON
var indexPath = Path.Combine(blogDataDir, "posts-index.json");
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
};
var indexJson = JsonSerializer.Serialize(allPostMetadata, jsonOptions);
File.WriteAllText(indexPath, indexJson);
Console.WriteLine($"Wrote posts index: {indexPath} ({allPostMetadata.Count} posts)");

// Generate RSS feed with full post content
var feedPath = Path.Combine(outputDir, "feed.xml");
var rssXml = RssGenerator.Generate(
    title: "Observer Magazine",
    description: "A free, open-source Blazor WebAssembly showcase on .NET 10",
    siteUrl: "https://observermagazine.github.io",
    posts: allPostMetadata,
    getPostHtml: slug => postHtmlMap.GetValueOrDefault(slug)
);
File.WriteAllText(feedPath, rssXml);
Console.WriteLine($"Wrote RSS feed: {feedPath}");

Console.WriteLine("Content processing complete.");

// --- Types used by the index ---
public sealed class PostIndexEntry
{
    public string Slug { get; init; } = "";
    public string Title { get; init; } = "";
    public DateTime Date { get; init; }
    public string Author { get; init; } = "";
    public string Summary { get; init; } = "";
    public string[] Tags { get; init; } = [];
}
