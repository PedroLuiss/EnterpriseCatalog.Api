using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnterpriseCatalog.Api.Data;
using EnterpriseCatalog.Api.DTOs;
using EnterpriseCatalog.Api.Models;
using EnterpriseCatalog.Api.Services;

namespace EnterpriseCatalog.Api.Controllers
{
    // Define la ruta base del controlador. ASP.NET Core reemplaza [controller]
    // por el nombre de la clase sin el sufijo "Controller": /api/auth.
    [Route("api/[controller]")]

    // Activa convenciones para APIs, como la vinculacion de datos de la solicitud
    // y respuestas automaticas cuando falla la validacion del modelo.
    // Para que la validacion detecte reglas, estas deben declararse en los DTO.
    [ApiController]
    public class AuthController : ControllerBase
    {
        // Contexto de Entity Framework Core: permite consultar y guardar usuarios.
        private readonly AppDbContext _context;

        // Servicio de la aplicacion que crea los tokens JWT para usuarios autenticados.
        private readonly TokenService _tokenService;

        // ASP.NET Core crea el controlador e inyecta estas dependencias desde
        // el contenedor configurado en Program.cs.
        public AuthController(AppDbContext context, TokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        // Atiende solicitudes HTTP POST a /api/auth/register.
        // [ApiController] permite vincular el cuerpo JSON de la solicitud con RegisterDto.
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            // BCrypt solo usa los primeros 72 bytes UTF-8. Rechazar contrasenas
            // mas largas evita que dos valores distintos terminen siendo equivalentes.
            if (Encoding.UTF8.GetByteCount(dto.Password) > 72)
            {
                ModelState.AddModelError(
                    nameof(dto.Password),
                    "La contraseña no puede superar los 72 bytes en UTF-8.");
                return ValidationProblem(ModelState);
            }

            // Guardar una forma canonica hace consistentes las comparaciones de correo
            // cuando el cliente envia letras mayusculas.
            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

            // Comprueba en la base de datos si ya hay un usuario con este correo.
            // AnyAsync devuelve true si encuentra al menos uno, sin cargar el usuario
            // completo. await espera la consulta sin bloquear el hilo durante la espera.
            if (await _context.Users.AnyAsync(u => u.Email == normalizedEmail))
            {
                // Conflict genera HTTP 409 cuando el correo ya esta en uso.
                return Conflict(new { message = "El correo ya está registrado." });
            }

            // Crea la entidad que Entity Framework Core guardara en la tabla Users.
            var user = new User
            {
                Email = normalizedEmail,

                // BCrypt genera un hash unidireccional de la contrasena.
                // Se almacena el hash, no la contrasena original; al iniciar sesion,
                // BCrypt.Verify compara la contrasena recibida con este hash.
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };

            // Agrega la entidad al seguimiento del contexto. La insercion en SQL
            // se realiza al llamar a SaveChangesAsync.
            _context.Users.Add(user);

            // Persiste los cambios pendientes en SQL Server de forma asincrona.
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
            {
                // El indice unico tambien protege contra dos registros simultaneos
                // que hayan superado la consulta AnyAsync.
                return Conflict(new { message = "El correo ya está registrado." });
            }

            // Ok produce HTTP 200 y ASP.NET Core serializa el mensaje como JSON.
            return Ok(new { message = "Usuario registrado con éxito." });
        }

        // Atiende solicitudes HTTP POST a /api/auth/login.
        // Recibe las credenciales del cuerpo JSON mediante LoginDto.
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            // BCrypt solo verifica de forma segura hasta 72 bytes UTF-8.
            if (Encoding.UTF8.GetByteCount(dto.Password) > 72)
            {
                ModelState.AddModelError(
                    nameof(dto.Password),
                    "La contraseña no puede superar los 72 bytes en UTF-8.");
                return ValidationProblem(ModelState);
            }

            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

            // Busca el primer usuario que tenga el correo indicado.
            // FirstOrDefaultAsync devuelve null si no hay coincidencias.
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            // Si el usuario no existe, no se intenta verificar una contrasena.
            // Si existe, BCrypt.Verify comprueba la contrasena recibida contra el hash.
            // Ambas causas producen la misma respuesta para no revelar si un correo
            // esta registrado.
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                // Unauthorized produce HTTP 401: las credenciales no son validas.
                return Unauthorized(new { message = "Credenciales incorrectas." });
            }

            // Las credenciales son correctas: el servicio crea un JWT para el usuario.
            var token = _tokenService.GenerateToken(user);

            // Devuelve HTTP 200 y el token en JSON. El cliente lo enviara en futuras
            // solicitudes que requieran autenticacion.
            return Ok(new { token });
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException exception)
        {
            return exception.GetBaseException() is SqlException sqlException
                && sqlException.Number is 2601 or 2627;
        }
    }
}