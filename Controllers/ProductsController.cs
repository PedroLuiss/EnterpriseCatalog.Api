using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using EnterpriseCatalog.Api.Data;
using EnterpriseCatalog.Api.DTOs;
using EnterpriseCatalog.Api.Models;

namespace EnterpriseCatalog.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private const long MaxImageSize = 5 * 1024 * 1024;
        private const int MaxPageSize = 100;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductsController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] bool includeInactive = false,
            CancellationToken cancellationToken = default)
        {
            if (page < 1 || pageSize < 1 || pageSize > MaxPageSize
                || page > int.MaxValue / pageSize)
                return BadRequest(new { message = $"La página debe ser mayor a cero y pageSize debe estar entre 1 y {MaxPageSize}." });

            if (categoryId is <= 0)
                return BadRequest(new { message = "categoryId debe ser mayor a cero." });

            if (search?.Length > 100)
                return BadRequest(new { message = "search no puede superar los 100 caracteres." });

            IQueryable<Product> query = _context.Products
                .AsNoTracking()
                .Include(product => product.Category)
                .Include(product => product.Variants);

            if (!includeInactive)
                query = query.Where(product => product.IsActive);

            if (categoryId.HasValue)
                query = query.Where(product => product.CategoryId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.Trim();
                query = query.Where(product =>
                    product.Name.Contains(searchTerm)
                    || (product.Sku != null && product.Sku.Contains(searchTerm))
                    || (product.Barcode != null && product.Barcode.Contains(searchTerm))
                    || (product.Brand != null && product.Brand.Contains(searchTerm))
                    || (product.Manufacturer != null && product.Manufacturer.Contains(searchTerm))
                    || (product.ModelNumber != null && product.ModelNumber.Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var products = await query
                .OrderBy(product => product.Name)
                .ThenBy(product => product.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return Ok(new
            {
                items = products.Select(ToResponse),
                page,
                pageSize,
                totalCount
            });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var product = await _context.Products
                .AsNoTracking()
                .Include(item => item.Category)
                .Include(item => item.Variants)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            return product is null
                ? NotFound(new { message = "No se encontró el producto." })
                : Ok(ToResponse(product));
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> Create(
            [FromForm] ProductCreateDto dto,
            CancellationToken cancellationToken)
        {
            if (!TryParseFlexibleFields(dto, out var attributes, out var tags, out var variants))
                return ValidationProblem(ModelState);

            if (dto.RemoveImage)
                return BadRequest(new { message = "RemoveImage solo se puede usar al actualizar un producto." });

            if (!await IsValidImageAsync(dto.Image, cancellationToken))
            {
                ModelState.AddModelError(nameof(dto.Image), "La imagen debe ser JPG, PNG, GIF o WEBP válida y no superar los 5 MB.");
                return ValidationProblem(ModelState);
            }

            if (!await _context.Categories.AnyAsync(category => category.Id == dto.CategoryId, cancellationToken))
                return BadRequest(new { message = "La categoría especificada no existe." });

            var duplicateVariantSku = variants
                .GroupBy(variant => variant.Sku.Trim(), StringComparer.OrdinalIgnoreCase)
                .Any(group => group.Count() > 1);
            if (duplicateVariantSku)
                return BadRequest(new { message = "Cada variante debe tener un SKU diferente." });

            var duplicateVariantBarcode = variants
                .Where(variant => !string.IsNullOrWhiteSpace(variant.Barcode))
                .GroupBy(variant => variant.Barcode!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Any(group => group.Count() > 1);
            if (duplicateVariantBarcode)
                return BadRequest(new { message = "Cada variante debe tener un código de barras diferente." });

            var imageUrl = await SaveImageAsync(dto.Image, cancellationToken);
            var product = new Product();
            ApplyInput(product, dto, attributes, tags, imageUrl);
            AddVariants(product, variants);

            _context.Products.Add(product);
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
            {
                DeleteImageFile(imageUrl);
                return Conflict(new { message = "El SKU o código de barras ya está registrado." });
            }
            catch (DbUpdateException)
            {
                DeleteImageFile(imageUrl);
                throw;
            }

            await _context.Entry(product).Reference(item => item.Category).LoadAsync(cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, ToResponse(product));
        }

        [HttpPut("{id:int}")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> Update(
            int id,
            [FromForm] ProductCreateDto dto,
            CancellationToken cancellationToken)
        {
            if (!TryParseFlexibleFields(dto, out var attributes, out var tags, out var variants))
                return ValidationProblem(ModelState);

            var product = await _context.Products
                .Include(item => item.Variants)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (product is null)
                return NotFound(new { message = "No se encontró el producto." });

            if (dto.RemoveImage && dto.Image is not null && dto.Image.Length > 0)
                return BadRequest(new { message = "Envía una imagen nueva o RemoveImage, pero no ambos." });

            if (!await _context.Categories.AnyAsync(category => category.Id == dto.CategoryId, cancellationToken))
                return BadRequest(new { message = "La categoría especificada no existe." });

            if (!await IsValidImageAsync(dto.Image, cancellationToken))
            {
                ModelState.AddModelError(nameof(dto.Image), "La imagen debe ser JPG, PNG, GIF o WEBP válida y no superar los 5 MB.");
                return ValidationProblem(ModelState);
            }

            var duplicateVariantSku = variants
                .GroupBy(variant => variant.Sku.Trim(), StringComparer.OrdinalIgnoreCase)
                .Any(group => group.Count() > 1);
            if (duplicateVariantSku)
                return BadRequest(new { message = "Cada variante debe tener un SKU diferente." });

            var duplicateVariantBarcode = variants
                .Where(variant => !string.IsNullOrWhiteSpace(variant.Barcode))
                .GroupBy(variant => variant.Barcode!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Any(group => group.Count() > 1);
            if (duplicateVariantBarcode)
                return BadRequest(new { message = "Cada variante debe tener un código de barras diferente." });

            var oldImageUrl = product.ImageUrl;
            var imageUrl = oldImageUrl;
            if (dto.RemoveImage)
                imageUrl = null;
            else if (dto.Image is not null && dto.Image.Length > 0)
                imageUrl = await SaveImageAsync(dto.Image, cancellationToken);

            ApplyInput(product, dto, attributes, tags, imageUrl);
            if (dto.VariantsJson is not null)
                SyncVariants(product, variants);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
            {
                if (imageUrl != oldImageUrl)
                    DeleteImageFile(imageUrl);
                return Conflict(new { message = "El SKU o código de barras ya está registrado." });
            }
            catch (DbUpdateException)
            {
                if (imageUrl != oldImageUrl)
                    DeleteImageFile(imageUrl);
                throw;
            }

            if (imageUrl != oldImageUrl)
                DeleteImageFile(oldImageUrl);

            await _context.Entry(product).Reference(item => item.Category).LoadAsync(cancellationToken);
            return Ok(ToResponse(product));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var product = await _context.Products
                .Include(item => item.Variants)
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

            if (product is null)
                return NotFound(new { message = "No se encontró el producto." });

            product.IsActive = false;
            product.UpdatedAtUtc = DateTime.UtcNow;
            foreach (var variant in product.Variants)
            {
                variant.IsActive = false;
                variant.UpdatedAtUtc = product.UpdatedAtUtc;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        private async Task<string?> SaveImageAsync(IFormFile? image, CancellationToken cancellationToken)
        {
            if (image is null || image.Length == 0)
                return null;

            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            var uploadsFolder = Path.Combine(
                _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                "uploads");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            await using (var fileStream = new FileStream(
                filePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                useAsync: true))
            {
                await image.CopyToAsync(fileStream, cancellationToken);
            }

            return $"/uploads/{fileName}";
        }

        private static async Task<bool> IsValidImageAsync(IFormFile? image, CancellationToken cancellationToken)
        {
            if (image is null || image.Length == 0)
                return true;

            if (image.Length > MaxImageSize)
                return false;

            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            return IsSupportedImageExtension(extension)
                && await HasValidImageSignatureAsync(image, extension, cancellationToken);
        }

        private static bool IsSupportedImageExtension(string extension) =>
            extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp";

        private static async Task<bool> HasValidImageSignatureAsync(
            IFormFile image,
            string extension,
            CancellationToken cancellationToken)
        {
            var header = new byte[12];
            await using var stream = image.OpenReadStream();
            var bytesRead = await stream.ReadAsync(header.AsMemory(), cancellationToken);

            return extension switch
            {
                ".jpg" or ".jpeg" => bytesRead >= 3
                    && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                ".png" => bytesRead >= 8
                    && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
                ".gif" => bytesRead >= 6
                    && (System.Text.Encoding.ASCII.GetString(header, 0, 6) is "GIF87a" or "GIF89a"),
                ".webp" => bytesRead >= 12
                    && System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF"
                    && System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WEBP",
                _ => false
            };
        }

        private bool TryParseFlexibleFields(
            ProductCreateDto dto,
            out Dictionary<string, JsonElement> attributes,
            out List<string> tags,
            out List<ProductVariantInputDto> variants)
        {
            attributes = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            tags = new List<string>();
            variants = new List<ProductVariantInputDto>();

            try
            {
                using var document = JsonDocument.Parse(dto.AttributesJson);
                if (document.RootElement.ValueKind != JsonValueKind.Object
                    || document.RootElement.EnumerateObject().Count() > 100)
                {
                    ModelState.AddModelError(nameof(dto.AttributesJson), "Los atributos deben ser un objeto JSON con hasta 100 propiedades.");
                }
                else
                {
                    foreach (var property in document.RootElement.EnumerateObject())
                    {
                        if (string.IsNullOrWhiteSpace(property.Name) || property.Name.Length > 64)
                        {
                            ModelState.AddModelError(nameof(dto.AttributesJson), "Cada nombre de atributo debe tener entre 1 y 64 caracteres.");
                            break;
                        }

                        attributes[property.Name] = property.Value.Clone();
                    }
                }
            }
            catch (JsonException)
            {
                ModelState.AddModelError(nameof(dto.AttributesJson), "Los atributos no contienen JSON válido.");
            }

            try
            {
                using var document = JsonDocument.Parse(dto.TagsJson);
                if (document.RootElement.ValueKind != JsonValueKind.Array
                    || document.RootElement.GetArrayLength() > 100)
                {
                    ModelState.AddModelError(nameof(dto.TagsJson), "Las etiquetas deben ser un arreglo JSON con hasta 100 elementos.");
                }
                else
                {
                    foreach (var item in document.RootElement.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.String
                            || string.IsNullOrWhiteSpace(item.GetString())
                            || item.GetString()!.Length > 40)
                        {
                            ModelState.AddModelError(nameof(dto.TagsJson), "Cada etiqueta debe ser texto de entre 1 y 40 caracteres.");
                            break;
                        }

                        tags.Add(item.GetString()!.Trim());
                    }
                }
            }
            catch (JsonException)
            {
                ModelState.AddModelError(nameof(dto.TagsJson), "Las etiquetas no contienen JSON válido.");
            }

            if (dto.VariantsJson is not null)
            {
                try
                {
                    using var document = JsonDocument.Parse(dto.VariantsJson);
                    if (document.RootElement.ValueKind != JsonValueKind.Array)
                    {
                        ModelState.AddModelError(nameof(dto.VariantsJson), "Las variantes deben enviarse como un arreglo JSON.");
                    }
                    else if (document.RootElement.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.Object))
                    {
                        ModelState.AddModelError(nameof(dto.VariantsJson), "Cada elemento de variantes debe ser un objeto JSON.");
                    }
                    else
                    {
                        variants = JsonSerializer.Deserialize<List<ProductVariantInputDto>>(
                            dto.VariantsJson,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ProductVariantInputDto>();
                    }

                    if (variants.Count > 100)
                        ModelState.AddModelError(nameof(dto.VariantsJson), "Un producto puede tener como máximo 100 variantes.");

                    for (var index = 0; index < variants.Count; index++)
                    {
                        var variant = variants[index];
                        if (string.IsNullOrWhiteSpace(variant.Sku) || variant.Sku.Length > 64
                            || string.IsNullOrWhiteSpace(variant.Name) || variant.Name.Length > 120
                            || variant.Stock < 0
                            || variant.Price is < 0 or > 9999999999999999.99m
                            || variant.Barcode?.Length > 64
                            || variant.Options is null
                            || variant.Options.Count > 50
                            || variant.Options.Any(option =>
                                string.IsNullOrWhiteSpace(option.Key)
                                || option.Key.Length > 64
                                || option.Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null))
                        {
                            ModelState.AddModelError(nameof(dto.VariantsJson), $"La variante en la posición {index} contiene datos inválidos.");
                        }
                    }
                }
                catch (JsonException)
                {
                    ModelState.AddModelError(nameof(dto.VariantsJson), "Las variantes no contienen JSON válido.");
                }
            }

            return ModelState.IsValid;
        }

        private static void ApplyInput(
            Product product,
            ProductCreateDto dto,
            Dictionary<string, JsonElement> attributes,
            List<string> tags,
            string? imageUrl)
        {
            product.Name = dto.Name.Trim();
            product.Description = dto.Description.Trim();
            product.Price = dto.Price!.Value;
            product.Stock = dto.Stock!.Value;
            product.CategoryId = dto.CategoryId!.Value;
            product.Sku = NormalizeIdentifier(dto.Sku);
            product.Barcode = NormalizeIdentifier(dto.Barcode);
            product.Brand = NormalizeOptionalText(dto.Brand);
            product.Manufacturer = NormalizeOptionalText(dto.Manufacturer);
            product.ModelNumber = NormalizeOptionalText(dto.ModelNumber);
            product.Condition = dto.Condition.Trim();
            product.CurrencyCode = dto.CurrencyCode.ToUpperInvariant();
            product.CostPrice = dto.CostPrice;
            product.CompareAtPrice = dto.CompareAtPrice;
            product.Weight = dto.Weight;
            product.WeightUnit = NormalizeOptionalText(dto.WeightUnit);
            product.Length = dto.Length;
            product.Width = dto.Width;
            product.Height = dto.Height;
            product.DimensionUnit = NormalizeOptionalText(dto.DimensionUnit);
            product.AttributesJson = JsonSerializer.Serialize(attributes);
            product.TagsJson = JsonSerializer.Serialize(tags);
            product.IsActive = dto.IsActive;
            product.ImageUrl = imageUrl;
            product.UpdatedAtUtc = product.Id == 0 ? null : DateTime.UtcNow;
        }

        private static void AddVariants(Product product, IEnumerable<ProductVariantInputDto> variants)
        {
            foreach (var input in variants)
            {
                product.Variants.Add(new ProductVariant
                {
                    Sku = input.Sku.Trim().ToUpperInvariant(),
                    Barcode = NormalizeIdentifier(input.Barcode),
                    Name = input.Name.Trim(),
                    OptionsJson = JsonSerializer.Serialize(input.Options),
                    Price = input.Price,
                    Stock = input.Stock,
                    IsActive = input.IsActive
                });
            }
        }

        private static void SyncVariants(Product product, List<ProductVariantInputDto> inputs)
        {
            var pendingInputs = inputs.ToDictionary(
                input => input.Sku.Trim(),
                StringComparer.OrdinalIgnoreCase);

            foreach (var current in product.Variants.ToList())
            {
                if (!pendingInputs.Remove(current.Sku, out var input))
                {
                    product.Variants.Remove(current);
                    continue;
                }

                UpdateVariant(current, input);
            }

            AddVariants(product, pendingInputs.Values);
        }

        private static void UpdateVariant(ProductVariant variant, ProductVariantInputDto input)
        {
            variant.Sku = input.Sku.Trim().ToUpperInvariant();
            variant.Barcode = NormalizeIdentifier(input.Barcode);
            variant.Name = input.Name.Trim();
            variant.OptionsJson = JsonSerializer.Serialize(input.Options);
            variant.Price = input.Price;
            variant.Stock = input.Stock;
            variant.IsActive = input.IsActive;
            variant.UpdatedAtUtc = DateTime.UtcNow;
        }

        private static ProductResponseDto ToResponse(Product product)
        {
            return new ProductResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                ImageUrl = product.ImageUrl,
                Sku = product.Sku,
                Barcode = product.Barcode,
                Brand = product.Brand,
                Manufacturer = product.Manufacturer,
                ModelNumber = product.ModelNumber,
                Condition = product.Condition,
                CurrencyCode = product.CurrencyCode,
                CostPrice = product.CostPrice,
                CompareAtPrice = product.CompareAtPrice,
                Weight = product.Weight,
                WeightUnit = product.WeightUnit,
                Length = product.Length,
                Width = product.Width,
                Height = product.Height,
                DimensionUnit = product.DimensionUnit,
                Attributes = DeserializeObject(product.AttributesJson),
                Tags = DeserializeList(product.TagsJson),
                IsActive = product.IsActive,
                CreatedAtUtc = product.CreatedAtUtc,
                UpdatedAtUtc = product.UpdatedAtUtc,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name,
                Variants = product.Variants.Select(variant => new ProductVariantResponseDto
                {
                    Id = variant.Id,
                    Sku = variant.Sku,
                    Barcode = variant.Barcode,
                    Name = variant.Name,
                    Options = DeserializeObject(variant.OptionsJson),
                    Price = variant.Price,
                    Stock = variant.Stock,
                    IsActive = variant.IsActive
                }).ToList()
            };
        }

        private static Dictionary<string, JsonElement> DeserializeObject(string json) =>
            JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
            ?? new Dictionary<string, JsonElement>();

        private static List<string> DeserializeList(string json) =>
            JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();

        private void DeleteImageFile(string? imageUrl)
        {
            const string uploadPrefix = "/uploads/";
            if (imageUrl is null || !imageUrl.StartsWith(uploadPrefix, StringComparison.Ordinal))
                return;

            var fileName = imageUrl[uploadPrefix.Length..];
            if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName)
                return;

            var uploadsFolder = Path.GetFullPath(Path.Combine(
                _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                "uploads"));
            var imagePath = Path.GetFullPath(Path.Combine(uploadsFolder, fileName));
            if (imagePath.StartsWith(uploadsFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                && System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }
        }

        private static string? NormalizeIdentifier(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

        private static string? NormalizeOptionalText(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
            exception.GetBaseException() is SqlException sqlException
            && sqlException.Number is 2601 or 2627;

    }
}
