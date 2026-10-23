namespace EnterpriseCatalog.Api.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string? ImageUrl { get; set; } // Ruta o URL de la foto del producto

        // Llave foránea de Categoría
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
    }
}