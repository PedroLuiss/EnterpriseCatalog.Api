using System.ComponentModel.DataAnnotations;

namespace EnterpriseCatalog.Api.DTOs
{
    public record RegisterDto(
        [property: Required(ErrorMessage = "El correo es obligatorio.")]
        [property: EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [property: StringLength(254, ErrorMessage = "El correo no puede superar los 254 caracteres.")]
        string Email,

        [property: Required(ErrorMessage = "La contraseña es obligatoria.")]
        [property: StringLength(72, MinimumLength = 12, ErrorMessage = "La contraseña debe tener entre 12 y 72 caracteres.")]
        string Password);

    public record LoginDto(
        [property: Required(ErrorMessage = "El correo es obligatorio.")]
        [property: EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [property: StringLength(254, ErrorMessage = "El correo no puede superar los 254 caracteres.")]
        string Email,

        [property: Required(ErrorMessage = "La contraseña es obligatoria.")]
        [property: StringLength(72, ErrorMessage = "La contraseña no puede superar los 72 caracteres.")]
        string Password);
}