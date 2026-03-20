// using Bunit;
// using Microsoft.Extensions.DependencyInjection;
// using ObserverMagazine.Web.Components;
// using Xunit;
//
// namespace ObserverMagazine.Web.Tests.Components;
//
// public class ResponsiveTableTests : TestContext
// {
//     private const string SampleProductsJson = """
//         [
//           { "name": "Widget A", "category": "Tools", "price": 9.99, "stock": 100, "rating": 4.5, "description": "A widget" },
//           { "name": "Gadget B", "category": "Electronics", "price": 49.99, "stock": 25, "rating": 3.8, "description": "A gadget" }
//         ]
//         """;
//
//     [Fact]
//     public void RendersLoadingState_Initially()
//     {
//         // The component fetches data on init; before data loads, it shows loading
//         var fakeHandler = new FakeHttpHandler(SampleProductsJson, "application/json");
//         var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("https://test.local/") };
//         Services.AddSingleton(httpClient);
//
//         var cut = RenderComponent<ResponsiveTable>();
//
//         // After render with async data, table should appear
//         cut.WaitForElement(".data-table");
//         Assert.Contains("Widget A", cut.Markup);
//         Assert.Contains("Gadget B", cut.Markup);
//     }
//
//     [Fact]
//     public void FilterInput_FiltersRows()
//     {
//         var fakeHandler = new FakeHttpHandler(SampleProductsJson, "application/json");
//         var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("https://test.local/") };
//         Services.AddSingleton(httpClient);
//
//         var cut = RenderComponent<ResponsiveTable>();
//         cut.WaitForElement(".data-table");
//
//         var filterInput = cut.Find(".filter-input");
//         filterInput.Input("Widget");
//
//         Assert.Contains("Widget A", cut.Markup);
//         Assert.DoesNotContain("Gadget B", cut.Markup);
//     }
//
//     [Fact]
//     public void ClickColumnHeader_SortsData()
//     {
//         var fakeHandler = new FakeHttpHandler(SampleProductsJson, "application/json");
//         var httpClient = new HttpClient(fakeHandler) { BaseAddress = new Uri("https://test.local/") };
//         Services.AddSingleton(httpClient);
//
//         var cut = RenderComponent<ResponsiveTable>();
//         cut.WaitForElement(".data-table");
//
//         // Click Price header to sort
//         var priceHeader = cut.FindAll("th.sortable")[2]; // Price is 3rd column
//         priceHeader.Click();
//
//         // After clicking, should show sort indicator
//         Assert.Contains("▲", cut.Markup);
//     }
// }
//
// // Reuse the FakeHttpHandler from BlogServiceTests
// internal sealed class FakeHttpHandler : HttpMessageHandler
// {
//     private readonly HttpResponseMessage response;
//
//     public FakeHttpHandler(string content, string mediaType)
//     {
//         response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
//         {
//             Content = new StringContent(content, System.Text.Encoding.UTF8, mediaType)
//         };
//     }
//
//     protected override Task<HttpResponseMessage> SendAsync(
//         HttpRequestMessage request, CancellationToken cancellationToken)
//     {
//         return Task.FromResult(response);
//     }
// }
