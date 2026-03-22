using System.Text.Json.Serialization;

namespace ObserverMagazine.Web.Models;

public sealed record Product
{
    /// <summary>
    /// Unique ID for CRUD identity. Auto-generated when not present in JSON.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public double Rating { get; set; }
    public string Description { get; set; } = "";
}
