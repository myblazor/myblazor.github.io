using ObserverMagazine.Web.Models;

namespace ObserverMagazine.Web.Services;

/// <summary>
/// Generates deterministic sample product data for the showcase catalog.
/// Uses a seeded Random so the same 2000 products are generated every time.
/// </summary>
public static class ProductDataGenerator
{
    private static readonly string[] Adjectives =
    [
        "Premium", "Classic", "Essential", "Ultra", "Pro", "Elite", "Compact",
        "Advanced", "Basic", "Deluxe", "Slim", "Turbo", "Eco", "Smart", "Vintage",
        "Modern", "Rugged", "Portable", "Wireless", "Ergonomic", "Heavy-Duty",
        "Lightweight", "Industrial", "Professional", "Everyday", "Travel", "Mini",
        "Mega", "Flex", "Rapid", "Silent", "Solar", "Titanium", "Carbon", "Nano"
    ];

    private static readonly string[] Colors =
    [
        "Black", "White", "Silver", "Navy", "Red", "Blue", "Green", "Gray",
        "Rose Gold", "Matte Black", "Space Gray", "Forest Green", "Ocean Blue",
        "Pearl White", "Crimson", "Teal", "Champagne", "Slate", "Ivory",
        "Graphite", "Copper", "Olive", "Sand", "Midnight", "Arctic White",
        "Charcoal", "Burgundy", "Cobalt", "Amber", "Lavender"
    ];

    private static readonly string[] Statuses = ["Active", "Active", "Active", "Active", "Discontinued", "Draft"];

    private static readonly (string Category, string[] Products, string[] Brands, decimal MinPrice, decimal MaxPrice, double MinWeight, double MaxWeight, int MinWarranty, int MaxWarranty)[] Categories =
    [
        ("Electronics", ["Keyboard", "Mouse", "Monitor", "Headphones", "Speaker", "Webcam", "Microphone", "Charger", "USB Hub", "SSD", "Flash Drive", "Router", "Earbuds", "Power Bank", "Docking Station", "Trackpad", "Stylus Pen", "Card Reader", "Surge Protector", "Streaming Box"],
         ["TechVibe", "SwiftGear", "NexGen", "ClearView", "VoltEdge", "PixelCore", "ZenByte", "ByteForge", "Quantum", "SonicEdge"],
         19.99m, 899.99m, 0.05, 12.0, 6, 36),

        ("Furniture", ["Standing Desk", "Office Chair", "Bookshelf", "Filing Cabinet", "Coffee Table", "Desk Lamp", "Bar Stool", "Monitor Stand", "Keyboard Tray", "Footrest", "Side Table", "Storage Ottoman", "Coat Rack", "Plant Stand", "Magazine Rack"],
         ["ZenFlow", "CoreMade", "IronForge", "ArcLine", "OakCraft", "SteelFrame", "NordicNest", "UrbanForm", "TimberLine", "VelvetRidge"],
         29.99m, 1299.99m, 1.5, 45.0, 3, 60),

        ("Clothing", ["T-Shirt", "Hoodie", "Jacket", "Running Shoes", "Cap", "Socks Pack", "Polo Shirt", "Cargo Pants", "Rain Coat", "Beanie", "Gloves", "Scarf", "Vest", "Shorts", "Sneakers"],
         ["PeakForm", "SkyBound", "GreenLeaf", "PurePath", "TrailBlaze", "UrbanThread", "FlexWear", "StormGear", "PaceLine", "VitalMove"],
         9.99m, 299.99m, 0.1, 2.5, 0, 12),

        ("Home & Kitchen", ["Blender", "Knife Set", "Cutting Board", "Dutch Oven", "Electric Kettle", "Toaster", "Food Scale", "Mixing Bowls", "Storage Containers", "Spice Rack", "Wine Opener", "Colander", "Rolling Pin", "Thermometer", "Timer"],
         ["SilkWave", "BlueShift", "FreshField", "SteelChef", "HomeEdge", "PureCook", "VividKitchen", "HarvestPro", "GrainMill", "BrightHome"],
         7.99m, 349.99m, 0.2, 8.0, 3, 24),

        ("Sports & Outdoors", ["Dumbbell Set", "Yoga Mat", "Resistance Bands", "Water Bottle", "Hiking Backpack", "Cycling Gloves", "Fitness Tracker", "Jump Rope", "Foam Roller", "Camping Tent", "Sleeping Bag", "Trekking Poles", "Swim Goggles", "Bike Lock", "Climbing Harness"],
         ["PeakForm", "SkyBound", "TrailBlaze", "IronForge", "VitalMove", "SummitGear", "RapidStride", "AquaPulse", "GritZone", "BoulderForce"],
         4.99m, 499.99m, 0.1, 15.0, 0, 24),

        ("Office Supplies", ["Pen Set", "Notebook", "Planner", "Stapler", "Desk Organizer", "Label Maker", "Calculator", "Whiteboard", "Marker Set", "Binder Clips", "Paper Shredder", "Tape Dispenser", "Envelope Pack", "Sticky Notes", "Pencil Case"],
         ["CoreMade", "ClearView", "ArcLine", "BlueShift", "PurePath", "DeskPrime", "InkWell", "PageCraft", "ScribeLine", "SharpPoint"],
         2.99m, 199.99m, 0.05, 10.0, 0, 12),

        ("Accessories", ["Phone Case", "Laptop Bag", "Desk Mat", "Cable Organizer", "Screen Protector", "Wallet", "Watch Band", "Sunglasses", "Belt", "Key Holder", "Passport Cover", "Wrist Rest", "Mousepad", "Webcam Cover", "Ring Holder"],
         ["TechVibe", "SilkWave", "PurePath", "ArcLine", "VoltEdge", "CaseCraft", "SnapShield", "LinkLoop", "WrapArt", "GripLine"],
         3.99m, 149.99m, 0.02, 2.0, 0, 12)
    ];

    public static List<CatalogProduct> Generate(int count = 2000, int seed = 42)
    {
        var rng = new Random(seed);
        var products = new List<CatalogProduct>(count);
        var baseDate = new DateTime(2024, 1, 1);
        var usedSkus = new HashSet<string>();

        for (int i = 0; i < count; i++)
        {
            var catEntry = Categories[rng.Next(Categories.Length)];
            var adj = Adjectives[rng.Next(Adjectives.Length)];
            var productType = catEntry.Products[rng.Next(catEntry.Products.Length)];
            var brand = catEntry.Brands[rng.Next(catEntry.Brands.Length)];
            var color = Colors[rng.Next(Colors.Length)];
            var name = $"{adj} {productType}";

            // Generate unique SKU
            string sku;
            do
            {
                sku = $"{catEntry.Category[..3].ToUpperInvariant()}-{rng.Next(10000, 99999)}";
            } while (!usedSkus.Add(sku));

            var priceRange = catEntry.MaxPrice - catEntry.MinPrice;
            var price = Math.Round(catEntry.MinPrice + (decimal)(rng.NextDouble() * (double)priceRange), 2);

            // ~30% chance of having a compare-at price (sale)
            decimal? compareAt = rng.NextDouble() < 0.3
                ? Math.Round(price * (1m + (decimal)(rng.NextDouble() * 0.4 + 0.1)), 2)
                : null;

            var weightRange = catEntry.MaxWeight - catEntry.MinWeight;
            var weight = Math.Round(catEntry.MinWeight + rng.NextDouble() * weightRange, 2);

            var warrantyRange = catEntry.MaxWarranty - catEntry.MinWarranty;
            var warranty = catEntry.MinWarranty + rng.Next(warrantyRange + 1);

            var rating = Math.Round(2.5 + rng.NextDouble() * 2.5, 1);
            var reviewCount = rng.Next(0, 2500);
            var stock = rng.Next(0, 1000);
            var daysAgo = rng.Next(0, 730);
            var status = Statuses[rng.Next(Statuses.Length)];

            products.Add(new CatalogProduct
            {
                Id = CreateDeterministicGuid(seed, i),
                Sku = sku,
                Name = name,
                Brand = brand,
                Category = catEntry.Category,
                Price = price,
                CompareAtPrice = compareAt,
                Stock = stock,
                Rating = rating,
                ReviewCount = reviewCount,
                WeightKg = weight,
                Color = color,
                WarrantyMonths = warranty,
                DateAdded = baseDate.AddDays(daysAgo),
                Status = status,
                Description = $"{adj} {productType.ToLowerInvariant()} by {brand}. Available in {color.ToLowerInvariant()}. {(warranty > 0 ? $"Includes {warranty}-month warranty." : "No warranty.")}"
            });
        }

        return products;
    }

    /// <summary>
    /// Creates a deterministic GUID from a seed and index so we get the same IDs every time.
    /// </summary>
    private static Guid CreateDeterministicGuid(int seed, int index)
    {
        var bytes = new byte[16];
        BitConverter.GetBytes(seed).CopyTo(bytes, 0);
        BitConverter.GetBytes(index).CopyTo(bytes, 4);
        // Fill rest with a hash-like pattern
        BitConverter.GetBytes(seed ^ (index * 2654435761)).CopyTo(bytes, 8);
        BitConverter.GetBytes(index ^ (seed * 2246822519)).CopyTo(bytes, 12);
        return new Guid(bytes);
    }
}
