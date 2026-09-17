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
            var productosBajoStock = await _context.Products.Where(p => p.Stock <= p.StockMinimo).ToListAsync();
            var totalVentas = await _context.MovimientosFinancieros.Where(m => m.Tipo == "Ingreso").SumAsync(m => m.Monto);
            var totalCompras = await _context.MovimientosFinancieros.Where(m => m.Tipo == "Egreso").SumAsync(m => m.Monto);

            ViewBag.BajoStock = productosBajoStock;
            ViewBag.Ingresos = totalVentas;
            ViewBag.Egresos = totalCompras;
            ViewBag.Balance = totalVentas - totalCompras;

            return View();
        }

        // --- Gestión de Proveedores ---
        public async Task<IActionResult> Proveedores()
        {
            return View(await _context.Proveedores.ToListAsync());
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
                return RedirectToAction(nameof(Proveedores));
            }
            return View(proveedor);
        }

        // Otros metodos como Edit, Delete, y Gestion de Cuentas de Usuarios irían aquí...
    }
}
