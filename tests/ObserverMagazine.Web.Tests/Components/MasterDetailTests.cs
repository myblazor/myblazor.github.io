using Bunit;
using Microsoft.Extensions.DependencyInjection;
using ObserverMagazine.Web.Components;
using ObserverMagazine.Web.Services;
using ObserverMagazine.Web.Tests.Services;
using Xunit;

namespace ObserverMagazine.Web.Tests.Components;

public class MasterDetailTests : IDisposable
{
    private readonly BunitContext ctx = new();

    private const string SampleJson = """
        [
          { "name": "Alpha", "category": "Cat1", "price": 10.00, "stock": 5, "rating": 4.0, "description": "Alpha desc" },
          { "name": "Bravo", "category": "Cat2", "price": 20.00, "stock": 10, "rating": 3.5, "description": "Bravo desc" }
        ]
        """;

    private void SetupServices()
    {
        var handler = new MasterDetailFakeHandler(SampleJson);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.local/") };
        ctx.Services.AddSingleton(httpClient);
        ctx.Services.AddSingleton<IAnalyticsService, NoOpAnalyticsService>();
    }

    [Fact]
    public void ShowsEmptyDetailPanel_WhenNothingSelected()
    {
        SetupServices();

        var cut = ctx.Render<MasterDetail>();
        cut.WaitForElement(".md-list ul");

        Assert.Contains("Select an item", cut.Markup);
    }

    [Fact]
    public void ClickingItem_ShowsDetails()
    {
        SetupServices();

        var cut = ctx.Render<MasterDetail>();
        cut.WaitForElement(".md-list ul");

        var firstItem = cut.Find(".md-list li");
        firstItem.Click();

        Assert.Contains("Alpha desc", cut.Markup);
    }

    [Fact]
    public void ClickingDifferentItem_UpdatesDetails()
    {
        SetupServices();

        var cut = ctx.Render<MasterDetail>();
        cut.WaitForElement(".md-list ul");

        var items = cut.FindAll(".md-list li");
        items[1].Click();

        Assert.Contains("Bravo desc", cut.Markup);
    }

    public void Dispose() => ctx.Dispose();
}

internal sealed class MasterDetailFakeHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage response;

    public MasterDetailFakeHandler(string json)
    {
        response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(response);
    }
}
