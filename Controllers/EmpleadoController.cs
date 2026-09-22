using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proyecto_asp.Data;
using proyecto_asp.Models;
using proyecto_asp.Services;

namespace proyecto_asp.Controllers
{
    [Authorize(Roles = "Admin,Empleado")]
    public class EmpleadoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly FacturaService _facturaService;

        public EmpleadoController(ApplicationDbContext context, FacturaService facturaService)
        {
            _context = context;
            _facturaService = facturaService;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Categorias = await _context.Categorias.Where(c => c.Activa).OrderBy(c => c.Id).ToListAsync();
            ViewBag.Productos = await _context.Products.OrderBy(p => p.Name).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarPedido(int[] productIds, int[] cantidades, string metodoPago, string? nombreCliente)
        {
            if (productIds == null || cantidades == null || productIds.Length == 0 || productIds.Length != cantidades.Length)
            {
                TempData["Error"] = "Debe seleccionar al menos un producto para registrar el pedido.";
                return RedirectToAction(nameof(Index));
            }

            if (metodoPago != "Efectivo" && metodoPago != "Tarjeta")
            {
                metodoPago = "Efectivo";
            }

            decimal totalPedido = 0;
            var detalles = new List<DetallePedido>();

            for (int i = 0; i < productIds.Length; i++)
            {
                int pId = productIds[i];
                int cant = cantidades[i];
                if (cant <= 0) continue;

                var prod = await _context.Products.FindAsync(pId);
                if (prod != null)
                {
                    decimal subtotal = prod.Price * cant;
                    totalPedido += subtotal;

                    detalles.Add(new DetallePedido
                    {
                        ProductId = prod.Id,
                        Cantidad = cant,
                        PrecioUnitario = prod.Price
                    });

                    // Descontar stock
                    prod.Stock = Math.Max(0, prod.Stock - cant);
                    _context.Products.Update(prod);
                }
            }

            if (!detalles.Any())
            {
                TempData["Error"] = "No se procesó ningún producto válido.";
                return RedirectToAction(nameof(Index));
            }

            string clienteInfo = string.IsNullOrWhiteSpace(nombreCliente) ? "Cliente en Local" : nombreCliente.Trim();

            var pedido = new Pedido
            {
                UserId = null, // Venta presencial en local por empleado
                FechaPedido = DateTime.UtcNow,
                Total = totalPedido,
                MetodoPago = metodoPago,
                Estado = "Completado",
                Detalles = detalles
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            // Registrar movimiento financiero de ingreso
            var ingreso = new MovimientoFinanciero
            {
                Tipo = "Ingreso",
                Descripcion = $"Venta en caja (Empleado - {clienteInfo}) — Pedido #{pedido.Id} ({metodoPago})",
                Monto = totalPedido,
                Fecha = DateTime.UtcNow,
                ReferenciaId = pedido.Id,
                EntidadReferencia = "Pedido"
            };
            _context.MovimientosFinancieros.Add(ingreso);
            await _context.SaveChangesAsync();

            TempData["Exito"] = $"¡Pedido #{pedido.Id} para '{clienteInfo}' registrado con éxito! Total: Bs. {totalPedido:F2} ({metodoPago}).";
            TempData["UltimoPedidoId"] = pedido.Id;

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Historial()
        {
            var ventasEmpleado = await _context.Pedidos
                .Where(p => p.UserId == null)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Product)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            return View(ventasEmpleado);
        }

        [HttpGet]
        public async Task<IActionResult> DescargarFacturaPdf(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.User)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
            {
                TempData["Error"] = "El pedido especificado no existe.";
                return RedirectToAction(nameof(Index));
            }

            byte[] pdfBytes = _facturaService.GenerarFacturaPdf(pedido);
            return File(pdfBytes, "application/pdf", $"Factura_Pedido_{id}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> DescargarFacturaExcel(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.User)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
            {
                TempData["Error"] = "El pedido especificado no existe.";
                return RedirectToAction(nameof(Index));
            }

            byte[] excelBytes = _facturaService.GenerarFacturaExcel(pedido);
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Factura_Pedido_{id}.xlsx");
        }
    }
}
