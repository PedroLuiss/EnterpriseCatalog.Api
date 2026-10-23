using Microsoft.AspNetCore.Http;

namespace EnterpriseCatalog.Api.DTOs
{
    public class ProductCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public int CategoryId { get; set; }
        public IFormFile? Image { get; set; } // Archivo de foto enviado desde el cliente
    }
}