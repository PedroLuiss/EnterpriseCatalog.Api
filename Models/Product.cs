namespace EnterpriseCatalog.Api.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string? ImageUrl { get; set; }
        public string? Sku { get; set; }
        public string? Barcode { get; set; }
        public string? Brand { get; set; }
        public string? Manufacturer { get; set; }
        public string? ModelNumber { get; set; }
        public string Condition { get; set; } = "New";
        public string CurrencyCode { get; set; } = "USD";
        public decimal? CostPrice { get; set; }
        public decimal? CompareAtPrice { get; set; }
        public decimal? Weight { get; set; }
        public string? WeightUnit { get; set; }
        public decimal? Length { get; set; }
        public decimal? Width { get; set; }
        public decimal? Height { get; set; }
        public string? DimensionUnit { get; set; }
        public string AttributesJson { get; set; } = "{}";
        public string TagsJson { get; set; } = "[]";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    }
}
