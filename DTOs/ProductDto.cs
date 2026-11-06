using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace EnterpriseCatalog.Api.DTOs
{
    public class ProductCreateDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(4000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(typeof(decimal), "0", "9999999999999999.99")]
        public decimal? Price { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int? Stock { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int? CategoryId { get; set; }

        [StringLength(64)]
        public string? Sku { get; set; }

        [StringLength(64)]
        public string? Barcode { get; set; }

        [StringLength(120)]
        public string? Brand { get; set; }

        [StringLength(120)]
        public string? Manufacturer { get; set; }

        [StringLength(120)]
        public string? ModelNumber { get; set; }

        [Required]
        [StringLength(40)]
        public string Condition { get; set; } = "New";

        [Required]
        [RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "La moneda debe indicarse con tres letras.")]
        public string CurrencyCode { get; set; } = "USD";

        [Range(typeof(decimal), "0", "9999999999999999.99")]
        public decimal? CostPrice { get; set; }

        [Range(typeof(decimal), "0", "9999999999999999.99")]
        public decimal? CompareAtPrice { get; set; }

        [Range(typeof(decimal), "0", "999999999999999.999")]
        public decimal? Weight { get; set; }

        [StringLength(20)]
        public string? WeightUnit { get; set; }

        [Range(typeof(decimal), "0", "999999999999999.999")]
        public decimal? Length { get; set; }

        [Range(typeof(decimal), "0", "999999999999999.999")]
        public decimal? Width { get; set; }

        [Range(typeof(decimal), "0", "999999999999999.999")]
        public decimal? Height { get; set; }

        [StringLength(20)]
        public string? DimensionUnit { get; set; }

        [StringLength(16000)]
        public string AttributesJson { get; set; } = "{}";

        [StringLength(8000)]
        public string TagsJson { get; set; } = "[]";

        public bool IsActive { get; set; } = true;

        public bool RemoveImage { get; set; }

        [StringLength(64000)]
        public string? VariantsJson { get; set; }

        public IFormFile? Image { get; set; }
    }

    public class ProductVariantInputDto
    {
        [Required]
        [StringLength(64)]
        public string Sku { get; set; } = string.Empty;

        [StringLength(64)]
        public string? Barcode { get; set; }

        [Required]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public Dictionary<string, JsonElement> Options { get; set; } = new();

        [Range(typeof(decimal), "0", "9999999999999999.99")]
        public decimal? Price { get; set; }

        [Range(0, int.MaxValue)]
        public int Stock { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class ProductResponseDto
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
        public string Condition { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public decimal? CostPrice { get; set; }
        public decimal? CompareAtPrice { get; set; }
        public decimal? Weight { get; set; }
        public string? WeightUnit { get; set; }
        public decimal? Length { get; set; }
        public decimal? Width { get; set; }
        public decimal? Height { get; set; }
        public string? DimensionUnit { get; set; }
        public Dictionary<string, JsonElement> Attributes { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public bool IsActive { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public List<ProductVariantResponseDto> Variants { get; set; } = new();
    }

    public class ProductVariantResponseDto
    {
        public int Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, JsonElement> Options { get; set; } = new();
        public decimal? Price { get; set; }
        public int Stock { get; set; }
        public bool IsActive { get; set; }
    }
}
