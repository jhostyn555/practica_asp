using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace proyecto_asp.Models
{
    public class Pedido
    {
        [Key]
        public int Id { get; set; }

        public string? UserId { get; set; } // Puede ser nulo si es una venta rápida en local
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [Required]
        public DateTime FechaPedido { get; set; } = DateTime.UtcNow;

        [Required, Range(0.01, 999999.99)]
        public decimal Total { get; set; }

        [Required, MaxLength(50)]
        public string MetodoPago { get; set; } = "Efectivo"; // Efectivo, QR, Tarjeta

        [Required, MaxLength(50)]
        public string Estado { get; set; } = "Completado"; // Completado, Pendiente, Cancelado

        // Relación 1 a muchos
        public ICollection<DetallePedido> Detalles { get; set; } = new List<DetallePedido>();
    }

    public class DetallePedido
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PedidoId { get; set; }
        [ForeignKey("PedidoId")]
        public Pedido Pedido { get; set; } = null!;

        [Required]
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;

        [Required, Range(1, int.MaxValue)]
        public int Cantidad { get; set; }

        [Required, Range(0.01, 999999.99)]
        public decimal PrecioUnitario { get; set; }
    }
}
