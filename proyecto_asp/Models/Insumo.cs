using System.ComponentModel.DataAnnotations;

namespace proyecto_asp.Models
{
    public class Insumo
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MaxLength(100)]
        [Display(Name = "Nombre del Insumo")]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(300)]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "La unidad de medida es obligatoria.")]
        [MaxLength(30)]
        [Display(Name = "Unidad de Medida")]
        public string Unidad { get; set; } = string.Empty; // ej: kg, litros, unidades

        [Required(ErrorMessage = "El stock actual es obligatorio.")]
        [Range(0, 999999.99, ErrorMessage = "El stock debe ser un valor positivo.")]
        [Display(Name = "Stock Actual")]
        public decimal StockActual { get; set; }

        [Required(ErrorMessage = "El stock mínimo es obligatorio.")]
        [Range(0, 999999.99, ErrorMessage = "El stock mínimo debe ser un valor positivo.")]
        [Display(Name = "Stock Mínimo")]
        public decimal StockMinimo { get; set; } = 5;

        [Required(ErrorMessage = "El costo unitario es obligatorio.")]
        [Range(0.01, 999999.99, ErrorMessage = "El costo debe ser mayor a 0.")]
        [Display(Name = "Costo Unitario (Bs.)")]
        public decimal CostoUnitario { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Propiedad calculada (no mapeada a BD)
        public bool TieneStockBajo => StockActual <= StockMinimo;
    }
}
