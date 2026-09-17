using System.ComponentModel.DataAnnotations;

namespace proyecto_asp.Models
{
    public class Proveedor
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Contacto { get; set; }

        [MaxLength(20)]
        public string? Telefono { get; set; }

        [MaxLength(200)]
        public string? Direccion { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
