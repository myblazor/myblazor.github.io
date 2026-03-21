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
            Author = "Test Author",
            Summary = "The first post",
            Tags = ["test", "intro"]
        },
        new()
        {
            Slug = "second-post",
            Title = "Second Post",
            Date = new DateTime(2026, 2, 20),
            Author = "Test Author",
            Summary = "The second post",
            Tags = ["test"]
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
        // Arrange
        var json = JsonSerializer.Serialize(SamplePosts,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var handler = new FakeHttpHandler(json, "application/json");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
        var service = CreateService(httpClient);

        // Act
        var posts = await service.GetPostsAsync();

        // Assert
        Assert.Equal(2, posts.Length);
        Assert.Equal("Second Post", posts[0].Title); // Feb 20 > Jan 15
        Assert.Equal("First Post", posts[1].Title);
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
}

/// <summary>
/// Simple fake HTTP handler for unit testing without external dependencies.
/// </summary>
internal sealed class FakeHttpHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage response;

    public FakeHttpHandler(string content, string mediaType)
    {
        response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content, System.Text.Encoding.UTF8, mediaType)
        };
    }

    public FakeHttpHandler(HttpStatusCode statusCode)
    {
        response = new HttpResponseMessage(statusCode);
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(response);
    }
}
