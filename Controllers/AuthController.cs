using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnterpriseCatalog.Api.Data;
using EnterpriseCatalog.Api.DTOs;
using EnterpriseCatalog.Api.Models;
using EnterpriseCatalog.Api.Services;

namespace EnterpriseCatalog.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly TokenService _tokenService;

        public AuthController(AppDbContext context, TokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
           if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest(new { message = "El correo ya está registrado." }); // Nota: corregir doble == a solo BadRequest

            var user = new User
            {
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Usuario registrado con éxito." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Credenciales incorrectas." });

            var token = _tokenService.GenerateToken(user);
            return Ok(new { token });
        }
    }
}