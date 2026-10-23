using System.Text.Json.Serialization;

namespace EnterpriseCatalog.Api.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        // Relación: Una Categoría tiene muchos Productos
        [JsonIgnore]
        public ICollection<Product>? Products { get; set; }
    }
}