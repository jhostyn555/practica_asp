using System.ComponentModel.DataAnnotations;

namespace proyecto_asp.Models
{
    public class Contacto
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string Mensaje { get; set; } = string.Empty;

        public DateTime FechaEnvio { get; set; } = DateTime.UtcNow;
        
        public bool Leido { get; set; } = false;
    }
}
