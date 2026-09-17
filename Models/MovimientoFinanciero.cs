using System.ComponentModel.DataAnnotations;

namespace proyecto_asp.Models
{
    public class MovimientoFinanciero
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Tipo { get; set; } = string.Empty; // "Ingreso" o "Egreso"

        [Required, MaxLength(255)]
        public string Descripcion { get; set; } = string.Empty;

        [Required, Range(0.01, 999999.99)]
        public decimal Monto { get; set; }

        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        // Opcional para relacionar de donde viene el movimiento
        public int? ReferenciaId { get; set; } // Puede ser el ID del Pedido o de la Compra
        [MaxLength(50)]
        public string? EntidadReferencia { get; set; } // "Pedido" o "Compra"
    }
}
