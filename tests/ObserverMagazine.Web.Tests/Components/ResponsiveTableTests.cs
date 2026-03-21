using Bunit;
using Microsoft.Extensions.DependencyInjection;
using ObserverMagazine.Web.Components;
using ObserverMagazine.Web.Services;
using Xunit;

namespace ObserverMagazine.Web.Tests.Components;

public class ResponsiveTableTests : Bunit.TestContext
{
    private const string SampleProductsJson = """
        [
          { "name": "Widget A", "category": "Tools", "price": 9.99, "stock": 100, "rating": 4.5, "description": "A widget" },
          { "name": "Gadget B", "category": "Electronics", "price": 49.99, "stock": 25, "rating": 3.8, "description": "A gadget" }
        ]
        """;

    private void SetupServices()
    {
        var fakeHandler = new ComponentFakeHttpHandler(SampleProductsJson);
        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("https://test.local/") };
        Services.AddSingleton(httpClient);
        Services.AddSingleton<IAnalyticsService, NoOpAnalyticsService>();
    }

    [Fact]
    public void RendersTable_WithData()
    {
        SetupServices();

        var cut = RenderComponent<ResponsiveTable>();
        cut.WaitForElement(".rt-data-table");

        Assert.Contains("Widget A", cut.Markup);
        Assert.Contains("Gadget B", cut.Markup);
    }

    [Fact]
    public void FilterInput_FiltersRows()
    {
        SetupServices();

        var cut = RenderComponent<ResponsiveTable>();
        cut.WaitForElement(".rt-data-table");

        var filterInput = cut.Find(".rt-filter-input");
        filterInput.Input("Widget");

        Assert.Contains("Widget A", cut.Markup);
        Assert.DoesNotContain("Gadget B", cut.Markup);
    }

    [Fact]
    public void ClickColumnHeader_SortsData()
    {
        SetupServices();

        var cut = RenderComponent<ResponsiveTable>();
        cut.WaitForElement(".rt-data-table");

        var priceHeader = cut.FindAll("th.rt-sortable")[2];
        priceHeader.Click();

        Assert.Contains("▲", cut.Markup);
    }
}

internal sealed class ComponentFakeHttpHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage response;

    public ComponentFakeHttpHandler(string json)
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
