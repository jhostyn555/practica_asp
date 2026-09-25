using System.ComponentModel.DataAnnotations;

namespace proyecto_asp.Models
{
    public class Categoria
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MaxLength(100)]
        [Display(Name = "Nombre de la Categoría")]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(50)]
        [Display(Name = "Ícono (Bootstrap Icons)")]
        public string? Icono { get; set; } = "bi-tag";

        public bool Activa { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
