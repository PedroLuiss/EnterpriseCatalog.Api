using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnterpriseCatalog.Api.Data;
using EnterpriseCatalog.Api.DTOs;
using EnterpriseCatalog.Api.Models;

namespace EnterpriseCatalog.Api.Controllers
{
    [Authorize] // Todo el catálogo requiere Token JWT
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductsController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _context.Products.Include(p => p.Category).ToListAsync();
            return Ok(products);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] ProductCreateDto dto)
        {
            // Validar si la categoría existe
            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == dto.CategoryId);
            if (!categoryExists) return BadRequest(new { message = "La categoría especificada no existe." });

            string? uniqueFileName = null;

            // Lógica de subida de la foto
            if (dto.Image != null && dto.Image.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads");
                
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                string extension = Path.GetExtension(dto.Image.FileName);
                uniqueFileName = $"{Guid.NewGuid()}{extension}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.Image.CopyToAsync(fileStream);
                }

                // Guardaremos la ruta relativa para acceder vía web
                uniqueFileName = $"/uploads/{uniqueFileName}";
            }

            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Stock = dto.Stock,
                CategoryId = dto.CategoryId,
                ImageUrl = uniqueFileName
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Producto creado con éxito", product });
        }
    }
}