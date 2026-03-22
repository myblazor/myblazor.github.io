using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using ObserverMagazine.Web.Components;
using ObserverMagazine.Web.Services;
using ObserverMagazine.Web.Tests.Services;
using Xunit;

namespace ObserverMagazine.Web.Tests.Components;

public class MasterDetailTests : IDisposable
{
    private readonly BunitContext ctx = new();

    private const string SampleProductsJson = """
        [
          { "name": "Widget A", "category": "Tools", "price": 9.99, "stock": 100, "rating": 4.5, "description": "A widget" },
          { "name": "Gadget B", "category": "Electronics", "price": 49.99, "stock": 25, "rating": 3.8, "description": "A gadget" }
        ]
        """;

    private void SetupServices()
    {
        var fakeHandler = new MasterDetailFakeHttpHandler(SampleProductsJson);
        var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("https://test.local/") };
        ctx.Services.AddSingleton(httpClient);
        ctx.Services.AddSingleton<IAnalyticsService>(new NoOpAnalyticsService());

        // bUnit provides JSInterop mocking automatically.
        // Set up the localStorage calls the component makes.
        ctx.JSInterop.SetupVoid("localStorage.setItem", _ => true);
        ctx.JSInterop.SetupVoid("localStorage.removeItem", _ => true);
        ctx.JSInterop.Setup<string?>("localStorage.getItem", _ => true).SetResult(null);
    }

    [Fact]
    public void MasterDetail_RendersProductList()
    {
        SetupServices();
        var cut = ctx.Render<MasterDetail>();

        // Wait for async load
        cut.WaitForState(() => cut.Markup.Contains("Widget A"));

        Assert.Contains("Widget A", cut.Markup);
        Assert.Contains("Gadget B", cut.Markup);
    }

    [Fact]
    public void MasterDetail_ShowsEmptyDetailOnLoad()
    {
        SetupServices();
        var cut = ctx.Render<MasterDetail>();

        cut.WaitForState(() => cut.Markup.Contains("Widget A"));

        Assert.Contains("Select an item", cut.Markup);
    }

    [Fact]
    public void MasterDetail_SelectProductShowsDetail()
    {
        SetupServices();
        var cut = ctx.Render<MasterDetail>();

        cut.WaitForState(() => cut.Markup.Contains("Widget A"));

        var firstItem = cut.Find(".md-list li");
        firstItem.Click();

        Assert.Contains("Tools", cut.Markup); // category
        Assert.Contains("Edit", cut.Markup);
        Assert.Contains("Delete", cut.Markup);
    }

    [Fact]
    public void MasterDetail_AddButtonShowsForm()
    {
        SetupServices();
        var cut = ctx.Render<MasterDetail>();

        cut.WaitForState(() => cut.Markup.Contains("Widget A"));

        var addBtn = cut.Find("button.btn-primary.btn-sm");
        addBtn.Click();

        Assert.Contains("New Product", cut.Markup);
        Assert.Contains("Save", cut.Markup);
        Assert.Contains("Cancel", cut.Markup);
    }

    [Fact]
    public void MasterDetail_HasResetButton()
    {
        SetupServices();
        var cut = ctx.Render<MasterDetail>();

        cut.WaitForState(() => cut.Markup.Contains("Widget A"));

        Assert.Contains("Reset to Defaults", cut.Markup);
    }

    public void Dispose() => ctx.Dispose();
}

internal sealed class MasterDetailFakeHttpHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage response;

    public MasterDetailFakeHttpHandler(string json)
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
