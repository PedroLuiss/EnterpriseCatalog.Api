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