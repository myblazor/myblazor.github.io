using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using ObserverMagazine.Web.Models;

namespace ObserverMagazine.Web.Services;

public sealed class BlogService(HttpClient http, ILogger<BlogService> logger) : IBlogService
{
    private BlogPostMetadata[]? _cachedIndex;
    private AuthorProfile[]? _cachedAuthors;

    public async Task<BlogPostMetadata[]> GetPostsAsync()
    {
        if (_cachedIndex is not null) return _cachedIndex;

        logger.LogInformation("Fetching blog posts index");
        try
        {
            var posts = await http.GetFromJsonAsync<BlogPostMetadata[]>("blog-data/posts-index.json");
            _cachedIndex = posts?
                .OrderByDescending(p => p.Date)
                .ToArray() ?? [];
            logger.LogInformation("Loaded {Count} blog posts", _cachedIndex.Length);
            return _cachedIndex;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Blog index not found — content processor may not have run");
            _cachedIndex = [];
            return _cachedIndex;
        }
    }

    public async Task<BlogPostMetadata?> GetPostMetadataAsync(string slug)
    {
        var posts = await GetPostsAsync();
        return posts.FirstOrDefault(p =>
            string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<string> GetPostHtmlAsync(string slug)
    {
        logger.LogInformation("Fetching HTML for post: {Slug}", slug);
        try
        {
            return await http.GetStringAsync($"blog-data/{slug}.html");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Failed to load HTML for post: {Slug}", slug);
            return "<p>Could not load post content.</p>";
        }
    }

    public async Task<AuthorProfile[]> GetAllAuthorsAsync()
    {
        if (_cachedAuthors is not null) return _cachedAuthors;

        logger.LogInformation("Fetching authors index");
        try
        {
            var authors = await http.GetFromJsonAsync<AuthorProfile[]>("blog-data/authors.json");
            _cachedAuthors = authors ?? [];
            logger.LogInformation("Loaded {Count} author profiles", _cachedAuthors.Length);
            return _cachedAuthors;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Authors index not found");
            _cachedAuthors = [];
            return _cachedAuthors;
        }
    }

    public async Task<AuthorProfile?> GetAuthorAsync(string authorId)
    {
        var authors = await GetAllAuthorsAsync();
        return authors.FirstOrDefault(a =>
            string.Equals(a.Id, authorId, StringComparison.OrdinalIgnoreCase));
    }
}
