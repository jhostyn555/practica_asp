using System.ComponentModel.DataAnnotations;

namespace proyecto_asp.Models
{
    public class Empleado
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        [MaxLength(100)]
        [Display(Name = "Nombre Completo")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Correo electrónico inválido.")]
        [MaxLength(100)]
        [Display(Name = "Correo Electrónico")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        [DataType(DataType.Password)]
        [MaxLength(100)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [MaxLength(20)]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [Required(ErrorMessage = "El cargo es obligatorio.")]
        [MaxLength(50)]
        [Display(Name = "Cargo")]
        public string Cargo { get; set; } = "Cajero / Atención";

        [Display(Name = "Estado")]
        public bool Activo { get; set; } = true;

        [Display(Name = "Fecha de Contratación")]
        public DateTime FechaContratacion { get; set; } = DateTime.UtcNow;
    }
}
