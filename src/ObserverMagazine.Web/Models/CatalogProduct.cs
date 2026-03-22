namespace ObserverMagazine.Web.Models;

/// <summary>
/// Rich product model for the showcase catalog demo.
/// More fields than the simple Product model to demonstrate
/// column visibility, multi-column filtering, and range filters.
/// </summary>
public sealed class CatalogProduct
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public string Brand { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public int Stock { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public double WeightKg { get; set; }
    public string Color { get; set; } = "";
    public int WarrantyMonths { get; set; }
    public DateTime DateAdded { get; set; }
    public string Status { get; set; } = "Active";
    public string Description { get; set; } = "";
}
