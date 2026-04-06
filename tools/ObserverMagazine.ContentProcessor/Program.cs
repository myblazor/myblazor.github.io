using System.Text.Json;
using ObserverMagazine.ContentProcessor;

// Parse command-line arguments
string contentDir = "content/blog";
string outputDir = "src/ObserverMagazine.Web/wwwroot";
string authorsDir = "content/authors";
DateTime publishBefore = DateTime.UtcNow;

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--content-dir" && i + 1 < args.Length)
        contentDir = args[++i];
    else if (args[i] == "--output-dir" && i + 1 < args.Length)
        outputDir = args[++i];
    else if (args[i] == "--authors-dir" && i + 1 < args.Length)
        authorsDir = args[++i];
    else if (args[i] == "--publish-before" && i + 1 < args.Length)
        publishBefore = DateTime.Parse(args[++i]);
}

Console.WriteLine($"Content directory: {contentDir}");
Console.WriteLine($"Output directory:  {outputDir}");
Console.WriteLine($"Authors directory: {authorsDir}");
Console.WriteLine($"Publish before:    {publishBefore:yyyy-MM-dd HH:mm:ss} UTC");

// --- Process author profiles ---
var authorProfiles = new Dictionary<string, AuthorProfile>(StringComparer.OrdinalIgnoreCase);

if (Directory.Exists(authorsDir))
{
    var authorFiles = Directory.GetFiles(authorsDir, "*.yml", SearchOption.TopDirectoryOnly);
    Console.WriteLine($"Found {authorFiles.Length} author profile(s)");

    foreach (var authorFile in authorFiles)
    {
        var authorId = Path.GetFileNameWithoutExtension(authorFile);
        var yamlContent = File.ReadAllText(authorFile);
        var profile = FrontMatterParser.ParseAuthor(yamlContent, authorId);

        if (profile is not null)
        {
            authorProfiles[authorId] = profile;
            Console.WriteLine($"  Loaded author: {authorId} ({profile.Name})");
        }
        else
        {
            Console.WriteLine($"  WARNING: Could not parse author file {authorFile}");
        }
    }
}
else
{
    Console.WriteLine($"Authors directory '{authorsDir}' does not exist. No author profiles loaded.");
}

// Write authors.json
string blogDataDir = Path.Combine(outputDir, "blog-data");
Directory.CreateDirectory(blogDataDir);

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
};

var authorsPath = Path.Combine(blogDataDir, "authors.json");
var authorsJson = JsonSerializer.Serialize(authorProfiles.Values.ToArray(), jsonOptions);
File.WriteAllText(authorsPath, authorsJson);
Console.WriteLine($"Wrote authors index: {authorsPath} ({authorProfiles.Count} authors)");

// --- Process blog posts ---
if (!Directory.Exists(contentDir))
{
    Console.WriteLine($"Content directory '{contentDir}' does not exist. Creating with no posts.");
    Directory.CreateDirectory(contentDir);
}

var markdownFiles = Directory.GetFiles(contentDir, "*.md", SearchOption.TopDirectoryOnly);
Console.WriteLine($"Found {markdownFiles.Length} markdown files");

var allPostMetadata = new List<PostIndexEntry>();
var postHtmlMap = new Dictionary<string, string>();
int skippedDrafts = 0;
int skippedFuture = 0;

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

    // Skip drafts
    if (frontMatter.Draft)
    {
        Console.WriteLine($"  SKIPPED: Draft post '{frontMatter.Title}'");
        skippedDrafts++;
        continue;
    }

    // Skip future-dated posts
    if (frontMatter.Date > publishBefore)
    {
        Console.WriteLine($"  SKIPPED: Future post '{frontMatter.Title}' (date: {frontMatter.Date:yyyy-MM-dd}, publish-before: {publishBefore:yyyy-MM-dd})");
        skippedFuture++;
        continue;
    }

    // Derive slug from filename
    var fileName = Path.GetFileNameWithoutExtension(mdFile);
    var slug = FrontMatterParser.DeriveSlug(fileName);

    var html = MarkdownProcessor.ToHtml(markdownBody);
    var readingTime = FrontMatterParser.CalculateReadingTime(markdownBody);

    // Resolve author display name
    var authorId = frontMatter.Author ?? "";
    var authorName = authorId;
    string? authorEmail = null;
    if (authorProfiles.TryGetValue(authorId, out var authorProfile))
    {
        authorName = authorProfile.Name;
        authorEmail = authorProfile.Email;
    }
    else if (!string.IsNullOrEmpty(authorId))
    {
        Console.WriteLine($"  WARNING: No author profile found for '{authorId}'");
    }

    // Write individual post HTML
    var htmlPath = Path.Combine(blogDataDir, $"{slug}.html");
    File.WriteAllText(htmlPath, html);
    Console.WriteLine($"  Wrote: {htmlPath} (~{readingTime} min read)");

    postHtmlMap[slug] = html;

    allPostMetadata.Add(new PostIndexEntry
    {
        Slug = slug,
        Title = frontMatter.Title,
        Date = frontMatter.Date,
        Updated = frontMatter.Updated,
        Author = authorId,
        AuthorName = authorName,
        AuthorEmail = authorEmail,
        Summary = frontMatter.Summary ?? "",
        Tags = frontMatter.Tags ?? [],
        Featured = frontMatter.Featured,
        Series = frontMatter.Series,
        Image = frontMatter.Image,
        ReadingTimeMinutes = readingTime
    });
}

// Sort by date descending
allPostMetadata.Sort((a, b) => b.Date.CompareTo(a.Date));

// Write posts index JSON
var indexPath = Path.Combine(blogDataDir, "posts-index.json");
var indexJson = JsonSerializer.Serialize(allPostMetadata, jsonOptions);
File.WriteAllText(indexPath, indexJson);
Console.WriteLine($"Wrote posts index: {indexPath} ({allPostMetadata.Count} posts, {skippedDrafts} drafts skipped, {skippedFuture} future posts skipped)");

// Generate RSS feed with full post content
var feedPath = Path.Combine(outputDir, "feed.xml");
var rssXml = RssGenerator.Generate(
    title: "My Blazor Magazine",
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
    public DateTime? Updated { get; init; }
    public string Author { get; init; } = "";
    public string AuthorName { get; init; } = "";
    public string? AuthorEmail { get; init; }
    public string Summary { get; init; } = "";
    public string[] Tags { get; init; } = [];
    public bool Featured { get; init; }
    public string? Series { get; init; }
    public string? Image { get; init; }
    public int ReadingTimeMinutes { get; init; }
}
