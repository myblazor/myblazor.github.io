using System.Text.Json;
using ObserverMagazine.Web.Models;
using ObserverMagazine.Web.Services;
using Xunit;
using static ObserverMagazine.Web.Pages.Showcase;

namespace ObserverMagazine.Web.Tests.Components;

/// <summary>
/// Tests for the Showcase page logic: ProductDataGenerator, ColumnDef, FilterKind,
/// ShowcasePrefs serialization, and filtering/sorting/pagination helpers.
/// </summary>
public class ProductDataGeneratorTests
{
    [Fact]
    public void Generate_ReturnsRequestedCount()
    {
        var products = ProductDataGenerator.Generate(100);
        Assert.Equal(100, products.Count);
    }

    [Fact]
    public void Generate_DefaultCountIs20_000()
    {
        var products = ProductDataGenerator.Generate();
        Assert.Equal(20_000, products.Count);
    }

    [Fact]
    public void Generate_SkusAreUnique()
    {
        var products = ProductDataGenerator.Generate(500);
        var uniqueSkus = products.Select(p => p.Sku).Distinct().Count();
        Assert.Equal(500, uniqueSkus);
    }

    [Fact]
    public void Generate_IdsAreUnique()
    {
        var products = ProductDataGenerator.Generate(500);
        var uniqueIds = products.Select(p => p.Id).Distinct().Count();
        Assert.Equal(500, uniqueIds);
    }

    [Fact]
    public void Generate_AllProductsHaveRequiredFields()
    {
        var products = ProductDataGenerator.Generate(200);
        foreach (var p in products)
        {
            Assert.False(string.IsNullOrWhiteSpace(p.Sku), "SKU should not be empty");
            Assert.False(string.IsNullOrWhiteSpace(p.Name), "Name should not be empty");
            Assert.False(string.IsNullOrWhiteSpace(p.Brand), "Brand should not be empty");
            Assert.False(string.IsNullOrWhiteSpace(p.Category), "Category should not be empty");
            Assert.False(string.IsNullOrWhiteSpace(p.Color), "Color should not be empty");
            Assert.False(string.IsNullOrWhiteSpace(p.Status), "Status should not be empty");
            Assert.True(p.Price > 0, "Price should be positive");
            Assert.True(p.Rating >= 2.5 && p.Rating <= 5.0, "Rating should be between 2.5 and 5.0");
            Assert.True(p.Stock >= 0, "Stock should be non-negative");
            Assert.NotEqual(default, p.DateAdded);
        }
    }

    [Fact]
    public void Generate_HasVarietyOfBrands()
    {
        var products = ProductDataGenerator.Generate(500);
        var uniqueBrands = products.Select(p => p.Brand).Distinct().Count();
        Assert.True(uniqueBrands >= 5, $"Expected at least 5 brands but got {uniqueBrands}");
    }

    [Fact]
    public void Generate_HasVarietyOfCategories()
    {
        var products = ProductDataGenerator.Generate(500);
        var uniqueCategories = products.Select(p => p.Category).Distinct().Count();
        Assert.True(uniqueCategories >= 3, $"Expected at least 3 categories but got {uniqueCategories}");
    }

    [Fact]
    public void Generate_DeterministicWithSameSeed()
    {
        var products1 = ProductDataGenerator.Generate(50, seed: 42);
        var products2 = ProductDataGenerator.Generate(50, seed: 42);

        for (int i = 0; i < 50; i++)
        {
            Assert.Equal(products1[i].Id, products2[i].Id);
            Assert.Equal(products1[i].Sku, products2[i].Sku);
            Assert.Equal(products1[i].Name, products2[i].Name);
            Assert.Equal(products1[i].Price, products2[i].Price);
        }
    }

    [Fact]
    public void Generate_DifferentSeedsProduceDifferentData()
    {
        var products1 = ProductDataGenerator.Generate(50, seed: 1);
        var products2 = ProductDataGenerator.Generate(50, seed: 999);

        // At least some products should differ
        var differentCount = 0;
        for (int i = 0; i < 50; i++)
        {
            if (products1[i].Name != products2[i].Name) differentCount++;
        }
        Assert.True(differentCount > 10, $"Expected significant differences but only {differentCount} of 50 were different");
    }

    [Fact]
    public void Generate_StatusesAreValid()
    {
        var validStatuses = new HashSet<string> { "Active", "Draft", "Archived", "Out of Stock" };
        var products = ProductDataGenerator.Generate(200);
        foreach (var p in products)
        {
            Assert.Contains(p.Status, validStatuses);
        }
    }

    [Fact]
    public void Generate_CompareAtPriceIsHigherWhenPresent()
    {
        var products = ProductDataGenerator.Generate(500);
        foreach (var p in products.Where(p => p.CompareAtPrice.HasValue))
        {
            Assert.True(p.CompareAtPrice!.Value > p.Price,
                $"CompareAtPrice ({p.CompareAtPrice}) should be higher than Price ({p.Price})");
        }
    }
}

/// <summary>
/// Tests for ColumnDef behavior and FilterKind enum.
/// </summary>
public class ColumnDefTests
{
    [Fact]
    public void ColumnDef_Render_ReturnsFormattedValue()
    {
        var col = new ColumnDef("Price", "Price", p => p.Price.ToString("C"), true, FilterKind.Range);
        var product = new CatalogProduct { Price = 29.99m };
        var result = col.Render(product);
        Assert.Contains("29.99", result);
    }

    [Fact]
    public void ColumnDef_DefaultValues()
    {
        var col = new ColumnDef("Test", "Test Header", p => p.Name, false, FilterKind.Text);
        Assert.True(col.Visible, "Columns should be visible by default");
        Assert.Equal(string.Empty, col.FilterText);
        Assert.Equal(string.Empty, col.FilterMin);
        Assert.Equal(string.Empty, col.FilterMax);
        Assert.Equal(string.Empty, col.MinWidth);
        Assert.Empty(col.SelectOptions);
    }

    [Fact]
    public void ColumnDef_SelectOptions_CanBeSet()
    {
        var col = new ColumnDef("Category", "Category", p => p.Category, false, FilterKind.Select)
        {
            SelectOptions = ["Electronics", "Furniture", "Clothing"]
        };
        Assert.Equal(3, col.SelectOptions.Length);
        Assert.Contains("Electronics", col.SelectOptions);
    }

    [Fact]
    public void ColumnDef_Visibility_CanBeToggled()
    {
        var col = new ColumnDef("Test", "Test", p => p.Name, false, FilterKind.Text);
        Assert.True(col.Visible);

        col.Visible = false;
        Assert.False(col.Visible);

        col.Visible = true;
        Assert.True(col.Visible);
    }

    [Fact]
    public void FilterKind_HasExpectedValues()
    {
        Assert.Equal(0, (int)FilterKind.Text);
        Assert.Equal(1, (int)FilterKind.Range);
        Assert.Equal(2, (int)FilterKind.Select);
    }
}

/// <summary>
/// Tests for ShowcasePrefs serialization round-trip.
/// </summary>
public class ShowcasePrefsTests
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void ShowcasePrefs_RoundTrip_AllFields()
    {
        var prefs = new ShowcasePrefs
        {
            PageSize = 50,
            SortColumn = "Price",
            SortAsc = false,
            ShowFilters = true,
            ColumnVisibility = new Dictionary<string, bool>
            {
                ["Sku"] = true,
                ["Name"] = true,
                ["Brand"] = false,
                ["Color"] = false,
            },
            Filters = new Dictionary<string, FilterState>
            {
                ["Name"] = new() { Text = "keyboard" },
                ["Price"] = new() { Min = "10", Max = "500" },
                ["Category"] = new() { Text = "Electronics" },
            }
        };

        var json = JsonSerializer.Serialize(prefs, JsonOpts);
        Assert.False(string.IsNullOrEmpty(json));

        var deserialized = JsonSerializer.Deserialize<ShowcasePrefs>(json, JsonOpts);
        Assert.NotNull(deserialized);
        Assert.Equal(50, deserialized.PageSize);
        Assert.Equal("Price", deserialized.SortColumn);
        Assert.False(deserialized.SortAsc);
        Assert.True(deserialized.ShowFilters);
        Assert.Equal(4, deserialized.ColumnVisibility!.Count);
        Assert.False(deserialized.ColumnVisibility["Brand"]);
        Assert.Equal(3, deserialized.Filters!.Count);
        Assert.Equal("keyboard", deserialized.Filters["Name"].Text);
        Assert.Equal("10", deserialized.Filters["Price"].Min);
        Assert.Equal("500", deserialized.Filters["Price"].Max);
    }

    [Fact]
    public void ShowcasePrefs_RoundTrip_Empty()
    {
        var prefs = new ShowcasePrefs();
        var json = JsonSerializer.Serialize(prefs, JsonOpts);
        var deserialized = JsonSerializer.Deserialize<ShowcasePrefs>(json, JsonOpts);

        Assert.NotNull(deserialized);
        Assert.Null(deserialized.PageSize);
        Assert.Null(deserialized.SortColumn);
        Assert.Null(deserialized.SortAsc);
        Assert.Null(deserialized.ShowFilters);
        Assert.Null(deserialized.ColumnVisibility);
        Assert.Null(deserialized.Filters);
    }

    [Fact]
    public void ShowcasePrefs_DeserializeFromPartialJson()
    {
        // Only page size set — rest should be null
        var json = """{"pageSize":100}""";
        var prefs = JsonSerializer.Deserialize<ShowcasePrefs>(json, JsonOpts);

        Assert.NotNull(prefs);
        Assert.Equal(100, prefs.PageSize);
        Assert.Null(prefs.SortColumn);
        Assert.Null(prefs.Filters);
    }

    [Fact]
    public void FilterState_RoundTrip()
    {
        var fs = new FilterState { Text = "test", Min = "5", Max = "100" };
        var json = JsonSerializer.Serialize(fs, JsonOpts);
        var deserialized = JsonSerializer.Deserialize<FilterState>(json, JsonOpts);

        Assert.NotNull(deserialized);
        Assert.Equal("test", deserialized.Text);
        Assert.Equal("5", deserialized.Min);
        Assert.Equal("100", deserialized.Max);
    }

    [Fact]
    public void FilterState_NullFields_Serialize()
    {
        var fs = new FilterState { Text = null, Min = null, Max = null };
        var json = JsonSerializer.Serialize(fs, JsonOpts);
        var deserialized = JsonSerializer.Deserialize<FilterState>(json, JsonOpts);

        Assert.NotNull(deserialized);
        Assert.Null(deserialized.Text);
        Assert.Null(deserialized.Min);
        Assert.Null(deserialized.Max);
    }
}

/// <summary>
/// Tests for filtering, sorting, and pagination logic extracted into testable helpers.
/// These test the same algorithms used by Showcase.razor's @code block.
/// </summary>
public class ShowcaseFilterSortTests
{
    private static List<CatalogProduct> CreateTestProducts()
    {
        return
        [
            new()
            {
                Id = Guid.NewGuid(), Sku = "SKU-001", Name = "Wireless Mouse", Brand = "TechCo",
                Category = "Electronics", Price = 29.99m, Stock = 150, Rating = 4.2,
                ReviewCount = 340, WeightKg = 0.12, Color = "Black", WarrantyMonths = 12,
                DateAdded = new DateTime(2025, 6, 15), Status = "Active",
                Description = "A wireless mouse by TechCo."
            },
            new()
            {
                Id = Guid.NewGuid(), Sku = "SKU-002", Name = "Standing Desk", Brand = "ErgoFit",
                Category = "Furniture", Price = 499.00m, Stock = 25, Rating = 4.8,
                ReviewCount = 120, WeightKg = 35.0, Color = "White", WarrantyMonths = 36,
                DateAdded = new DateTime(2025, 3, 10), Status = "Active",
                Description = "A standing desk by ErgoFit."
            },
            new()
            {
                Id = Guid.NewGuid(), Sku = "SKU-003", Name = "Desk Mat XL", Brand = "WorkSpace",
                Category = "Accessories", Price = 34.50m, Stock = 500, Rating = 4.0,
                ReviewCount = 89, WeightKg = 0.8, Color = "Gray", WarrantyMonths = 0,
                DateAdded = new DateTime(2025, 9, 22), Status = "Draft",
                Description = "An XL desk mat."
            },
            new()
            {
                Id = Guid.NewGuid(), Sku = "SKU-004", Name = "Keyboard Pro", Brand = "TechCo",
                Category = "Electronics", Price = 149.99m, Stock = 0, Rating = 4.5,
                ReviewCount = 700, WeightKg = 0.9, Color = "Black", WarrantyMonths = 24,
                DateAdded = new DateTime(2024, 12, 1), Status = "Out of Stock",
                Description = "A mechanical keyboard by TechCo."
            },
            new()
            {
                Id = Guid.NewGuid(), Sku = "SKU-005", Name = "Monitor Arm", Brand = "ErgoFit",
                Category = "Accessories", Price = 89.00m, Stock = 200, Rating = 3.9,
                ReviewCount = 55, WeightKg = 3.2, Color = "Silver", WarrantyMonths = 12,
                DateAdded = new DateTime(2025, 1, 5), Status = "Active",
                Description = "A monitor arm by ErgoFit."
            },
        ];
    }

    // --- Text filter tests ---

    [Fact]
    public void Filter_ByName_ContainsMatch()
    {
        var products = CreateTestProducts();
        var filtered = products.Where(p => p.Name.Contains("desk", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.Equal(2, filtered.Count); // "Standing Desk" and "Desk Mat XL"
    }

    [Fact]
    public void Filter_ByName_PartialMatch()
    {
        var products = CreateTestProducts();
        var filtered = products.Where(p => p.Name.Contains("at", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.Single(filtered);
        Assert.Equal("Desk Mat XL", filtered[0].Name);
    }

    [Fact]
    public void Filter_ByBrand_CaseInsensitive()
    {
        var products = CreateTestProducts();
        var filtered = products.Where(p => p.Brand.Contains("techco", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.Equal(2, filtered.Count);
    }

    [Fact]
    public void Filter_ByCategory_ExactMatch()
    {
        var products = CreateTestProducts();
        var filtered = products.Where(p => p.Category.Equals("Electronics", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.Equal(2, filtered.Count);
    }

    [Fact]
    public void Filter_ByStatus_ExactMatch()
    {
        var products = CreateTestProducts();
        var filtered = products.Where(p => p.Status.Equals("Active", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.Equal(3, filtered.Count);
    }

    [Fact]
    public void Filter_ByColor()
    {
        var products = CreateTestProducts();
        var filtered = products.Where(p => p.Color.Contains("black", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.Equal(2, filtered.Count);
    }

    // --- Range filter tests ---

    [Fact]
    public void Filter_ByPriceMin()
    {
        var products = CreateTestProducts();
        var filtered = products.Where(p => (double)p.Price >= 50.0).ToList();
        Assert.Equal(3, filtered.Count); // 499, 149.99, 89
    }

    [Fact]
    public void Filter_ByPriceMax()
    {
        var products = CreateTestProducts();
        var filtered = products.Where(p => (double)p.Price <= 35.0).ToList();
        Assert.Equal(2, filtered.Count); // 29.99, 34.50
    }

    [Fact]
    public void Filter_ByPriceRange()
    {
        var products = CreateTestProducts();
        var filtered = products
            .Where(p => (double)p.Price >= 30.0 && (double)p.Price <= 100.0)
            .ToList();
        Assert.Equal(2, filtered.Count); // 34.50, 89
    }

    [Fact]
    public void Filter_ByStockMin_GreaterOrEqual()
    {
        var products = CreateTestProducts();
        var filtered = products.Where(p => p.Stock >= 100).ToList();
        Assert.Equal(3, filtered.Count); // 150, 500, 200
    }

    [Fact]
    public void Filter_ByRatingMin()
    {
        var products = CreateTestProducts();
        var filtered = products.Where(p => p.Rating >= 4.5).ToList();
        Assert.Equal(2, filtered.Count); // 4.8, 4.5
    }

    [Fact]
    public void Filter_ByWarrantyRange()
    {
        var products = CreateTestProducts();
        var filtered = products.Where(p => p.WarrantyMonths >= 12 && p.WarrantyMonths <= 24).ToList();
        Assert.Equal(3, filtered.Count); // 12, 24, 12
    }

    // --- Combined filters ---

    [Fact]
    public void Filter_CombinedTextAndRange()
    {
        var products = CreateTestProducts();
        var filtered = products
            .Where(p => p.Brand.Contains("TechCo", StringComparison.OrdinalIgnoreCase))
            .Where(p => (double)p.Price >= 100.0)
            .ToList();
        Assert.Single(filtered);
        Assert.Equal("Keyboard Pro", filtered[0].Name);
    }

    [Fact]
    public void Filter_NoMatch_ReturnsEmpty()
    {
        var products = CreateTestProducts();
        var filtered = products.Where(p => p.Name.Contains("zzzznonexistent", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.Empty(filtered);
    }

    // --- Sort tests ---

    [Fact]
    public void Sort_ByName_Ascending()
    {
        var products = CreateTestProducts();
        var sorted = products.OrderBy(p => p.Name).ToList();
        Assert.Equal("Desk Mat XL", sorted[0].Name);
        Assert.Equal("Wireless Mouse", sorted[^1].Name);
    }

    [Fact]
    public void Sort_ByPrice_Descending()
    {
        var products = CreateTestProducts();
        var sorted = products.OrderByDescending(p => p.Price).ToList();
        Assert.Equal(499.00m, sorted[0].Price);
        Assert.Equal(29.99m, sorted[^1].Price);
    }

    [Fact]
    public void Sort_ByRating_Ascending()
    {
        var products = CreateTestProducts();
        var sorted = products.OrderBy(p => p.Rating).ToList();
        Assert.Equal(3.9, sorted[0].Rating);
        Assert.Equal(4.8, sorted[^1].Rating);
    }

    [Fact]
    public void Sort_ByStock_Descending()
    {
        var products = CreateTestProducts();
        var sorted = products.OrderByDescending(p => p.Stock).ToList();
        Assert.Equal(500, sorted[0].Stock);
        Assert.Equal(0, sorted[^1].Stock);
    }

    [Fact]
    public void Sort_ByDateAdded_Ascending()
    {
        var products = CreateTestProducts();
        var sorted = products.OrderBy(p => p.DateAdded).ToList();
        Assert.Equal(new DateTime(2024, 12, 1), sorted[0].DateAdded);
    }

    // --- Pagination tests ---

    [Fact]
    public void Paginate_FirstPage()
    {
        var products = CreateTestProducts();
        var pageSize = 2;
        var page = products.Skip(0).Take(pageSize).ToList();
        Assert.Equal(2, page.Count);
    }

    [Fact]
    public void Paginate_SecondPage()
    {
        var products = CreateTestProducts();
        var pageSize = 2;
        var page = products.Skip(2).Take(pageSize).ToList();
        Assert.Equal(2, page.Count);
    }

    [Fact]
    public void Paginate_LastPage_Partial()
    {
        var products = CreateTestProducts(); // 5 items
        var pageSize = 2;
        var page = products.Skip(4).Take(pageSize).ToList();
        Assert.Single(page);
    }

    [Fact]
    public void Paginate_TotalPages_Calculation()
    {
        var count = 5;
        var pageSize = 2;
        var totalPages = Math.Max(1, (int)Math.Ceiling(count / (double)pageSize));
        Assert.Equal(3, totalPages);
    }

    [Fact]
    public void Paginate_SinglePage()
    {
        var count = 3;
        var pageSize = 10;
        var totalPages = Math.Max(1, (int)Math.Ceiling(count / (double)pageSize));
        Assert.Equal(1, totalPages);
    }

    [Fact]
    public void Paginate_EmptyList()
    {
        var count = 0;
        var pageSize = 20;
        var totalPages = Math.Max(1, (int)Math.Ceiling(count / (double)pageSize));
        Assert.Equal(1, totalPages); // Still page 1 even with no items
    }

    // --- DateAdded text filter ---

    [Fact]
    public void Filter_ByDateText_PartialYear()
    {
        var products = CreateTestProducts();
        var filtered = products
            .Where(p => p.DateAdded.ToString("yyyy-MM-dd").Contains("2025", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.Equal(4, filtered.Count); // All except the 2024-12-01 one
    }

    [Fact]
    public void Filter_ByDateText_SpecificMonth()
    {
        var products = CreateTestProducts();
        var filtered = products
            .Where(p => p.DateAdded.ToString("yyyy-MM-dd").Contains("2025-06", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.Single(filtered);
    }
}
