using ObserverMagazine.Web.Models;
using ObserverMagazine.Web.Services;
using Xunit;

namespace ObserverMagazine.Web.Tests.Components;

public class ProductDataGeneratorTests
{
    [Fact]
    public void Generate_ReturnsRequestedCount()
    {
        var products = ProductDataGenerator.Generate(100);
        Assert.Equal(100, products.Count);
    }

    [Fact]
    public void Generate_DefaultCountIs2000()
    {
        var products = ProductDataGenerator.Generate();
        Assert.Equal(2000, products.Count);
    }

    [Fact]
    public void Generate_IsDeterministic()
    {
        var first = ProductDataGenerator.Generate(50, seed: 123);
        var second = ProductDataGenerator.Generate(50, seed: 123);

        for (int i = 0; i < first.Count; i++)
        {
            Assert.Equal(first[i].Id, second[i].Id);
            Assert.Equal(first[i].Name, second[i].Name);
            Assert.Equal(first[i].Price, second[i].Price);
        }
    }

    [Fact]
    public void Generate_AllProductsHaveRequiredFields()
    {
        var products = ProductDataGenerator.Generate(200);

        foreach (var p in products)
        {
            Assert.NotEqual(Guid.Empty, p.Id);
            Assert.False(string.IsNullOrWhiteSpace(p.Sku));
            Assert.False(string.IsNullOrWhiteSpace(p.Name));
            Assert.False(string.IsNullOrWhiteSpace(p.Brand));
            Assert.False(string.IsNullOrWhiteSpace(p.Category));
            Assert.True(p.Price >= 0);
            Assert.True(p.Stock >= 0);
            Assert.InRange(p.Rating, 0, 5);
            Assert.True(p.WeightKg >= 0);
            Assert.False(string.IsNullOrWhiteSpace(p.Color));
            Assert.True(p.WarrantyMonths >= 0);
            Assert.False(string.IsNullOrWhiteSpace(p.Status));
        }
    }

    [Fact]
    public void Generate_SkusAreUnique()
    {
        var products = ProductDataGenerator.Generate(2000);
        var uniqueSkus = products.Select(p => p.Sku).Distinct().Count();
        Assert.Equal(2000, uniqueSkus);
    }

    [Fact]
    public void Generate_HasVarietyOfCategories()
    {
        var products = ProductDataGenerator.Generate(2000);
        var categories = products.Select(p => p.Category).Distinct().ToList();
        Assert.True(categories.Count >= 5, $"Expected at least 5 categories but got {categories.Count}");
    }

    [Fact]
    public void Generate_HasVarietyOfBrands()
    {
        var products = ProductDataGenerator.Generate(2000);
        var brands = products.Select(p => p.Brand).Distinct().ToList();
        Assert.True(brands.Count >= 20, $"Expected at least 20 brands but got {brands.Count}");
    }

    [Fact]
    public void Generate_SomeProductsHaveCompareAtPrice()
    {
        var products = ProductDataGenerator.Generate(2000);
        var withCompare = products.Count(p => p.CompareAtPrice.HasValue);
        Assert.True(withCompare > 100, $"Expected many products with compare-at price but got {withCompare}");
        Assert.True(withCompare < 1500, $"Expected most products without compare-at price but got {withCompare}");
    }

    [Fact]
    public void Generate_CompareAtPriceHigherThanPrice()
    {
        var products = ProductDataGenerator.Generate(2000);
        foreach (var p in products.Where(p => p.CompareAtPrice.HasValue))
        {
            Assert.True(p.CompareAtPrice!.Value > p.Price,
                $"Product {p.Sku}: compare-at {p.CompareAtPrice} should be > price {p.Price}");
        }
    }

    [Fact]
    public void Generate_DifferentSeedsProduceDifferentData()
    {
        var a = ProductDataGenerator.Generate(10, seed: 1);
        var b = ProductDataGenerator.Generate(10, seed: 999);

        // Not all names should match
        var matchCount = a.Zip(b).Count(pair => pair.First.Name == pair.Second.Name);
        Assert.True(matchCount < 10, "Different seeds should produce different data");
    }
}
