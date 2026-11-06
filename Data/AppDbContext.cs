using Microsoft.EntityFrameworkCore;
using EnterpriseCatalog.Api.Models;

namespace EnterpriseCatalog.Api.Data
{
    // DbContext es la clase base de Entity Framework Core que representa una
    // sesion de trabajo con la base de datos. Desde aqui se consultan entidades
    // y se persisten los cambios realizados durante una solicitud.
    public class AppDbContext : DbContext
    {
        // ASP.NET Core entrega estas opciones mediante inyeccion de dependencias.
        // En Program.cs se configuran, entre otras cosas, el proveedor SQL Server
        // y la cadena de conexion. AddDbContext normalmente crea un contexto por solicitud.
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Cada DbSet representa una tabla o conjunto de entidades que EF Core
        // puede consultar y modificar. Por convencion, estas propiedades generan
        // las tablas Users, Categories y Products.
        //
        // Ejemplo: _context.Users.Where(u => u.Email == email) construye una
        // consulta que EF Core traduce a SQL; SaveChangesAsync persiste cambios.
        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }

        // EF Core llama este metodo al construir el modelo que relaciona las clases
        // C# con tablas, columnas, indices y relaciones de la base de datos.
        // Las configuraciones aqui se reflejan en las migraciones.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Conserva cualquier configuracion definida por DbContext o sus clases base.
            base.OnModelCreating(modelBuilder);

            // Configura Products.Price como decimal(18,2) en SQL Server:
            // 18 digitos en total y 2 decimales. Esto evita que EF Core use una
            // precision por defecto distinta para los precios.
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Product>()
                .Property(p => p.CostPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Product>()
                .Property(p => p.CompareAtPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Product>()
                .Property(p => p.Weight)
                .HasColumnType("decimal(18,3)");

            modelBuilder.Entity<Product>()
                .Property(p => p.Length)
                .HasColumnType("decimal(18,3)");

            modelBuilder.Entity<Product>()
                .Property(p => p.Width)
                .HasColumnType("decimal(18,3)");

            modelBuilder.Entity<Product>()
                .Property(p => p.Height)
                .HasColumnType("decimal(18,3)");

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Sku)
                .IsUnique();

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Barcode)
                .IsUnique();

            modelBuilder.Entity<Product>()
                .Property(p => p.Sku)
                .HasMaxLength(64);

            modelBuilder.Entity<Product>()
                .Property(p => p.Barcode)
                .HasMaxLength(64);

            modelBuilder.Entity<Product>()
                .Property(p => p.Brand)
                .HasMaxLength(120);

            modelBuilder.Entity<Product>()
                .Property(p => p.Manufacturer)
                .HasMaxLength(120);

            modelBuilder.Entity<Product>()
                .Property(p => p.ModelNumber)
                .HasMaxLength(120);

            modelBuilder.Entity<Product>()
                .Property(p => p.WeightUnit)
                .HasMaxLength(20);

            modelBuilder.Entity<Product>()
                .Property(p => p.DimensionUnit)
                .HasMaxLength(20);

            modelBuilder.Entity<Product>()
                .Property(p => p.CurrencyCode)
                .HasDefaultValue("USD")
                .HasMaxLength(3)
                .IsFixedLength();

            modelBuilder.Entity<Product>()
                .Property(p => p.Condition)
                .HasDefaultValue("New")
                .HasMaxLength(40);

            modelBuilder.Entity<Product>()
                .Property(p => p.AttributesJson)
                .HasDefaultValue("{}");

            modelBuilder.Entity<Product>()
                .Property(p => p.TagsJson)
                .HasDefaultValue("[]");

            modelBuilder.Entity<Product>()
                .Property(p => p.IsActive)
                .HasDefaultValue(true);

            modelBuilder.Entity<Product>()
                .Property(p => p.CreatedAtUtc)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            modelBuilder.Entity<Product>()
                .HasMany(p => p.Variants)
                .WithOne(v => v.Product)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductVariant>()
                .Property(v => v.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<ProductVariant>()
                .HasIndex(v => v.Sku)
                .IsUnique();

            modelBuilder.Entity<ProductVariant>()
                .HasIndex(v => v.Barcode)
                .IsUnique();

            modelBuilder.Entity<ProductVariant>()
                .Property(v => v.Sku)
                .HasMaxLength(64);

            modelBuilder.Entity<ProductVariant>()
                .Property(v => v.Barcode)
                .HasMaxLength(64);

            modelBuilder.Entity<ProductVariant>()
                .Property(v => v.Name)
                .HasMaxLength(120);

            modelBuilder.Entity<ProductVariant>()
                .Property(v => v.OptionsJson)
                .HasDefaultValue("{}");

            modelBuilder.Entity<ProductVariant>()
                .Property(v => v.IsActive)
                .HasDefaultValue(true);

            modelBuilder.Entity<ProductVariant>()
                .Property(v => v.CreatedAtUtc)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            // Limita el correo a 254 caracteres y configura el tamano maximo de
            // la columna en el esquema relacional. La validacion de entrada tambien
            // se declara en los DTO de autenticacion.
            modelBuilder.Entity<User>()
                .Property(u => u.Email)
                .HasMaxLength(254);

            // Crea una restriccion de unicidad para el correo: SQL Server rechazara
            // dos usuarios con el mismo valor, incluso si se registran simultaneamente.
            // Esta regla debe aplicarse a la base existente con una migracion.
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}