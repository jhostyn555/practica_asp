using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace proyecto_asp.Models
{
    public class Compra
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProveedorId { get; set; }
        [ForeignKey("ProveedorId")]
        public Proveedor Proveedor { get; set; } = null!;

        [Required]
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;

        [Required, Range(1, int.MaxValue)]
        public int Cantidad { get; set; }

        [Required, Range(0.01, 999999.99)]
        public decimal CostoTotal { get; set; }

        public DateTime FechaCompra { get; set; } = DateTime.UtcNow;
    }
}
