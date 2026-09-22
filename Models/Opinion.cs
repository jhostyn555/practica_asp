using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace proyecto_asp.Models
{
    public class Opinion
    {
        [Key]
        public int Id { get; set; }

        public string? UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public int? ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [Required, Range(1, 5)]
        public int Calificacion { get; set; } // Estrellas 1 a 5

        [MaxLength(1000)]
        public string? Comentario { get; set; }

        [MaxLength(100)]
        public string? NombrePublico { get; set; } // Nombre del visitante o cliente

        public DateTime Fecha { get; set; } = DateTime.UtcNow;
    }
}
