using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proyecto_asp.Data;
using proyecto_asp.Models;

namespace proyecto_asp.Controllers
{
    [Authorize(Roles = "Empleado,Admin")]
    public class EmpleadoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmpleadoController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> RegistroPedido()
        {
            ViewBag.Productos = await _context.Products.Where(p => p.Stock > 0).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarVenta(int productoId, int cantidad, string metodoPago)
        {
            var producto = await _context.Products.FindAsync(productoId);
            if (producto == null || producto.Stock < cantidad)
            {
                TempData["Error"] = "Producto no encontrado o stock insuficiente.";
                return RedirectToAction(nameof(RegistroPedido));
            }

            var total = producto.Price * cantidad;

            // 1. Crear Pedido y Detalle
            var pedido = new Pedido
            {
                Total = total,
                MetodoPago = metodoPago,
                Estado = "Completado"
            };
            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            var detalle = new DetallePedido
            {
                PedidoId = pedido.Id,
                ProductId = producto.Id,
                Cantidad = cantidad,
                PrecioUnitario = producto.Price
            };
            _context.DetallesPedido.Add(detalle);

            // 2. Descontar Stock
            producto.Stock -= cantidad;
            _context.Products.Update(producto);

            // 3. Registrar Movimiento Financiero
            var ingreso = new MovimientoFinanciero
            {
                Tipo = "Ingreso",
                Descripcion = $"Venta de {cantidad}x {producto.Name}",
                Monto = total,
                ReferenciaId = pedido.Id,
                EntidadReferencia = "Pedido"
            };
            _context.MovimientosFinancieros.Add(ingreso);

            await _context.SaveChangesAsync();
            TempData["Exito"] = $"Venta registrada correctamente. Factura #{pedido.Id} generada.";
            return RedirectToAction(nameof(RegistroPedido));
        }
    }
}
