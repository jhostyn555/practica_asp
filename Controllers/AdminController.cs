using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proyecto_asp.Data;
using proyecto_asp.Models;

namespace proyecto_asp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            // --- Alertas de stock ---
            var productosBajoStock = await _context.Products
                .Where(p => p.Stock <= p.StockMinimo)
                .OrderBy(p => p.Stock)
                .ToListAsync();

            // --- Totales financieros ---
            var totalVentas = await _context.MovimientosFinancieros
                .Where(m => m.Tipo == "Ingreso").SumAsync(m => m.Monto);
            var totalCompras = await _context.MovimientosFinancieros
                .Where(m => m.Tipo == "Egreso").SumAsync(m => m.Monto);

            // --- Pedidos del día ---
            var hoy = DateTime.UtcNow.Date;
            var manana = hoy.AddDays(1);
            var pedidosHoy = await _context.Pedidos
                .Where(p => p.FechaPedido >= hoy && p.FechaPedido < manana)
                .CountAsync();
            var ventasHoy = await _context.Pedidos
                .Where(p => p.FechaPedido >= hoy && p.FechaPedido < manana)
                .SumAsync(p => p.Total);

            // --- Últimos 5 pedidos ---
            var ultimosPedidos = await _context.Pedidos
                .Include(p => p.User)
                .Include(p => p.Detalles)
                .OrderByDescending(p => p.Id)
                .Take(5)
                .ToListAsync();

            // --- Total de pedidos registrados ---
            var totalPedidos = await _context.Pedidos.CountAsync();

            ViewBag.BajoStock = productosBajoStock;
            ViewBag.Ingresos = totalVentas;
            ViewBag.Egresos = totalCompras;
            ViewBag.Balance = totalVentas - totalCompras;
            ViewBag.PedidosHoy = pedidosHoy;
            ViewBag.VentasHoy = ventasHoy;
            ViewBag.UltimosPedidos = ultimosPedidos;
            ViewBag.TotalPedidos = totalPedidos;

            return View();
        }

        // --- Gestión de Proveedores (CRUD completo) ---
        public async Task<IActionResult> Proveedores()
        {
            return View(await _context.Proveedores.OrderBy(p => p.Id).ToListAsync());
        }

        public IActionResult CrearProveedor()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearProveedor(Proveedor proveedor)
        {
            if (ModelState.IsValid)
            {
                _context.Proveedores.Add(proveedor);
                await _context.SaveChangesAsync();
                TempData["Exito"] = $"Proveedor '{proveedor.Nombre}' creado correctamente.";
                return RedirectToAction(nameof(Proveedores));
            }
            return View(proveedor);
        }

        public async Task<IActionResult> EditarProveedor(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null) return NotFound();
            return View(proveedor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarProveedor(int id, Proveedor proveedor)
        {
            if (id != proveedor.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Proveedores.Update(proveedor);
                await _context.SaveChangesAsync();
                TempData["Exito"] = $"Proveedor '{proveedor.Nombre}' actualizado correctamente.";
                return RedirectToAction(nameof(Proveedores));
            }
            return View(proveedor);
        }

        public async Task<IActionResult> EliminarProveedor(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null) return NotFound();
            return View(proveedor);
        }

        [HttpPost, ActionName("EliminarProveedor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarProveedorConfirmado(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor != null)
            {
                _context.Proveedores.Remove(proveedor);
                await _context.SaveChangesAsync();
                TempData["Exito"] = "Proveedor eliminado correctamente.";
            }
            return RedirectToAction(nameof(Proveedores));
        }

        // --- Gestión de Productos (vista admin) ---
        public async Task<IActionResult> Productos()
        {
            var productos = await _context.Products.OrderBy(p => p.Id).ToListAsync();
            return View(productos);
        }

        // --- Historial de Pedidos de Clientes ---
        public async Task<IActionResult> HistorialCompras()
        {
            var pedidos = await _context.Pedidos
                .Include(p => p.User)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Product)
                .OrderBy(p => p.Id)
                .ToListAsync();
            return View(pedidos);
        }

        // Otros metodos como Edit, Delete, y Gestion de Cuentas de Usuarios irían aquí...
    }
}
