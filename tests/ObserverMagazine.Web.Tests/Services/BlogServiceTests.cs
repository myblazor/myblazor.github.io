using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using ObserverMagazine.Web.Models;
using ObserverMagazine.Web.Services;
using Xunit;

namespace ObserverMagazine.Web.Tests.Services;

public class BlogServiceTests
{
    private static readonly BlogPostMetadata[] SamplePosts =
    [
        new()
        {
            Slug = "first-post",
            Title = "First Post",
            Date = new DateTime(2026, 1, 15),
            Author = "observer-team",
            AuthorName = "Observer Team",
            Summary = "The first post",
            Tags = ["test", "intro"],
            ReadingTimeMinutes = 2,
            Featured = true
        },
        new()
        {
            Slug = "second-post",
            Title = "Second Post",
            Date = new DateTime(2026, 2, 20),
            Author = "observer-team",
            AuthorName = "Observer Team",
            Summary = "The second post",
            Tags = ["test"],
            ReadingTimeMinutes = 3
        }
    ];

    private static readonly AuthorProfile[] SampleAuthors =
    [
        new()
        {
            Id = "observer-team",
            Name = "Observer Team",
            Email = "hello@observermagazine.example",
            Bio = "The team behind Observer Magazine.",
            Socials = new Dictionary<string, string> { ["github"] = "ObserverMagazine" }
        }
    ];

    private static BlogService CreateService(HttpClient httpClient)
    {
        var logger = NullLogger<BlogService>.Instance;
        return new BlogService(httpClient, logger);
    }

    [Fact]
    public async Task GetPostsAsync_ReturnsPostsSortedByDateDescending()
    {
        var json = JsonSerializer.Serialize(SamplePosts,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var handler = new FakeHttpHandler(json, "application/json");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
        var service = CreateService(httpClient);

        var posts = await service.GetPostsAsync();

        Assert.Equal(2, posts.Length);
        Assert.Equal("Second Post", posts[0].Title);
        Assert.Equal("First Post", posts[1].Title);
    }

    [Fact]
    public async Task GetPostsAsync_IncludesEnhancedMetadata()
    {
        var json = JsonSerializer.Serialize(SamplePosts,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var handler = new FakeHttpHandler(json, "application/json");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
        var service = CreateService(httpClient);

        var posts = await service.GetPostsAsync();
        var featured = posts.FirstOrDefault(p => p.Featured);

        Assert.NotNull(featured);
        Assert.Equal("First Post", featured.Title);
        Assert.Equal(2, featured.ReadingTimeMinutes);
        Assert.Equal("observer-team", featured.Author);
        Assert.Equal("Observer Team", featured.AuthorName);
    }

    [Fact]
    public async Task GetPostMetadataAsync_ReturnsNullForUnknownSlug()
    {
        var json = JsonSerializer.Serialize(SamplePosts,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var handler = new FakeHttpHandler(json, "application/json");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
        var service = CreateService(httpClient);

        var result = await service.GetPostMetadataAsync("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPostMetadataAsync_FindsBySlug()
    {
        var json = JsonSerializer.Serialize(SamplePosts,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var handler = new FakeHttpHandler(json, "application/json");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
        var service = CreateService(httpClient);

        var result = await service.GetPostMetadataAsync("first-post");

        Assert.NotNull(result);
        Assert.Equal("First Post", result.Title);
    }

    [Fact]
    public async Task GetPostsAsync_ReturnsEmptyOnHttpError()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.NotFound);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
        var service = CreateService(httpClient);

        var posts = await service.GetPostsAsync();

        Assert.Empty(posts);
    }

    [Fact]
    public async Task GetAllAuthorsAsync_ReturnsAuthors()
    {
        var json = JsonSerializer.Serialize(SampleAuthors,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var handler = new FakeHttpHandler(json, "application/json");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
        var service = CreateService(httpClient);

        var authors = await service.GetAllAuthorsAsync();

        Assert.Single(authors);
        Assert.Equal("Observer Team", authors[0].Name);
    }

    [Fact]
    public async Task GetAuthorAsync_FindsById()
    {
        var json = JsonSerializer.Serialize(SampleAuthors,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var handler = new FakeHttpHandler(json, "application/json");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
        var service = CreateService(httpClient);

        var author = await service.GetAuthorAsync("observer-team");

        Assert.NotNull(author);
        Assert.Equal("hello@observermagazine.example", author.Email);
    }

    [Fact]
    public async Task GetAuthorAsync_ReturnsNullForUnknown()
    {
        var json = JsonSerializer.Serialize(SampleAuthors,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var handler = new FakeHttpHandler(json, "application/json");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
        var service = CreateService(httpClient);

        var author = await service.GetAuthorAsync("nonexistent");

        Assert.Null(author);
    }
}

/// <summary>
/// Simple fake HTTP handler for unit testing without external dependencies.
/// </summary>
internal sealed class FakeHttpHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;

    public FakeHttpHandler(string content, string mediaType)
    {
        _response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content, System.Text.Encoding.UTF8, mediaType)
        };
    }

    public FakeHttpHandler(HttpStatusCode statusCode)
    {
        _response = new HttpResponseMessage(statusCode);
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_response);
    }
}
