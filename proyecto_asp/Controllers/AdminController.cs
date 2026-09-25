using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proyecto_asp.Data;
using proyecto_asp.Models;

using Microsoft.AspNetCore.Identity;

namespace proyecto_asp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly proyecto_asp.Services.FacturaService _facturaService;
        private readonly proyecto_asp.Services.ReporteService _reporteService;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, proyecto_asp.Services.FacturaService facturaService, proyecto_asp.Services.ReporteService reporteService)
        {
            _context = context;
            _userManager = userManager;
            _facturaService = facturaService;
            _reporteService = reporteService;
        }

        public async Task<IActionResult> Dashboard()
        {
            // --- Alertas de stock ---
            var productosBajoStock = await _context.Products
                .Where(p => p.Stock <= p.StockMinimo)
                .OrderBy(p => p.Stock)
                .ToListAsync();

            // --- Ingresos = suma de todos los pedidos completados ---
            var totalIngresos = await _context.Pedidos.Where(p => p.Estado == "Completado").SumAsync(p => p.Total);

            // --- Egresos = valor total del inventario de insumos (costo * stock) ---
            var insumos = await _context.Insumos.ToListAsync();
            var totalEgresos = insumos.Sum(i => i.CostoUnitario * i.StockActual);

            // --- Valor total de productos (precio * stock) ---
            var productos = await _context.Products.ToListAsync();
            var valorProductos = productos.Sum(p => p.Price * p.Stock);

            // --- Pedidos del día ---
            var hoyUtc = DateTime.UtcNow;
            var hoy = new DateTime(hoyUtc.Year, hoyUtc.Month, hoyUtc.Day, 0, 0, 0, DateTimeKind.Utc);
            var manana = hoy.AddDays(1);
            var pedidosHoy = await _context.Pedidos
                .Where(p => p.FechaPedido >= hoy && p.FechaPedido < manana && p.Estado == "Completado")
                .CountAsync();
            var ventasHoy = await _context.Pedidos
                .Where(p => p.FechaPedido >= hoy && p.FechaPedido < manana && p.Estado == "Completado")
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
            ViewBag.Ingresos = totalIngresos;
            ViewBag.Egresos = totalEgresos;
            ViewBag.Balance = totalIngresos - totalEgresos;
            ViewBag.ValorProductos = valorProductos;
            ViewBag.PedidosHoy = pedidosHoy;
            ViewBag.VentasHoy = ventasHoy;
            ViewBag.UltimosPedidos = ultimosPedidos;
            ViewBag.TotalPedidos = totalPedidos;

            return View();
        }

        // --- POS Pedidos para el administrador ---
        public async Task<IActionResult> PosPedidos()
        {
            ViewBag.Categorias = await _context.Categorias.Where(c => c.Activa).OrderBy(c => c.Id).ToListAsync();
            ViewBag.Productos = await _context.Products.OrderBy(p => p.Name).ToListAsync();
            return View("~/Views/Empleado/Index.cshtml");
        }

        // --- Estado editable de pedidos (AJAX) ---
        [HttpPost]
        public async Task<IActionResult> ActualizarEstado(int id, string estado)
        {
            var estadosValidos = new[] { "Pendiente", "En camino", "Completado" };
            if (!estadosValidos.Contains(estado))
                return Json(new { success = false, message = "Estado inválido." });

            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null)
                return Json(new { success = false, message = "Pedido no encontrado." });

            pedido.Estado = estado;
            _context.Pedidos.Update(pedido);
            await _context.SaveChangesAsync();

            return Json(new { success = true, estado });
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

        // --- Comentarios de Clientes ---
        public async Task<IActionResult> ComentariosClientes()
        {
            var opiniones = await _context.Opiniones
                .Include(o => o.User)
                .OrderByDescending(o => o.Id)
                .ToListAsync();
            return View(opiniones);
        }

        // --- Mensajes de Contacto ---
        public async Task<IActionResult> Contacto()
        {
            var contactos = await _context.Contactos
                .OrderByDescending(c => c.Id)
                .ToListAsync();
            return View(contactos);
        }

        // --- CRUD de Categorías ---
        public async Task<IActionResult> Categorias()
        {
            var categorias = await _context.Categorias.OrderBy(c => c.Id).ToListAsync();
            return View(categorias);
        }

        public IActionResult CrearCategoria() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearCategoria(Categoria categoria)
        {
            if (ModelState.IsValid)
            {
                _context.Categorias.Add(categoria);
                await _context.SaveChangesAsync();
                TempData["Exito"] = $"Categoría '{categoria.Nombre}' creada correctamente.";
                return RedirectToAction(nameof(Categorias));
            }
            return View(categoria);
        }

        public async Task<IActionResult> EditarCategoria(int id)
        {
            var cat = await _context.Categorias.FindAsync(id);
            if (cat == null) return NotFound();
            return View(cat);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarCategoria(int id, Categoria categoria)
        {
            if (id != categoria.Id) return NotFound();

            var existing = await _context.Categorias.FindAsync(id);
            if (existing == null) return NotFound();

            if (ModelState.IsValid)
            {
                existing.Nombre = categoria.Nombre;
                existing.Icono = categoria.Icono;
                existing.Activa = categoria.Activa;
                _context.Categorias.Update(existing);
                await _context.SaveChangesAsync();
                TempData["Exito"] = $"Categoría '{categoria.Nombre}' actualizada correctamente.";
                return RedirectToAction(nameof(Categorias));
            }
            return View(categoria);
        }

        public async Task<IActionResult> EliminarCategoria(int id)
        {
            var cat = await _context.Categorias.FindAsync(id);
            if (cat == null) return NotFound();
            return View(cat);
        }

        // --- CRUD de Empleados ---
        public async Task<IActionResult> Empleados()
        {
            var empleados = await _context.Empleados.OrderBy(e => e.Id).ToListAsync();
            return View(empleados);
        }

        public IActionResult CrearEmpleado() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearEmpleado(Empleado empleado)
        {
            if (!ModelState.IsValid) return View(empleado);

            var identityUser = await _userManager.FindByEmailAsync(empleado.Email);

            if (identityUser == null)
            {
                identityUser = new ApplicationUser
                {
                    UserName = empleado.Email,
                    Email = empleado.Email,
                    FullName = empleado.Nombre,
                    EmailConfirmed = true
                };

                var createRes = await _userManager.CreateAsync(identityUser, empleado.Password);
                if (!createRes.Succeeded)
                {
                    foreach (var e in createRes.Errors)
                        ModelState.AddModelError(string.Empty, e.Description);
                    return View(empleado);   // NO guardamos el Empleado
                }
            }
            else
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(identityUser);
                var resetRes = await _userManager.ResetPasswordAsync(identityUser, token, empleado.Password);
                if (!resetRes.Succeeded)
                {
                    foreach (var e in resetRes.Errors)
                        ModelState.AddModelError(string.Empty, e.Description);
                    return View(empleado);
                }
            }

            if (!await _userManager.IsInRoleAsync(identityUser, "Empleado"))
            {
                var roleRes = await _userManager.AddToRoleAsync(identityUser, "Empleado");
                if (!roleRes.Succeeded)
                {
                    foreach (var e in roleRes.Errors)
                        ModelState.AddModelError(string.Empty, e.Description);
                    return View(empleado);
                }
            }

            empleado.Password = string.Empty;   // no guardar la contraseña en claro
            _context.Empleados.Add(empleado);
            await _context.SaveChangesAsync();

            TempData["Exito"] = $"Empleado '{empleado.Nombre}' registrado correctamente.";
            return RedirectToAction(nameof(Empleados));
        }

        public async Task<IActionResult> EditarEmpleado(int id)
        {
            var emp = await _context.Empleados.FindAsync(id);
            if (emp == null) return NotFound();
            return View(emp);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarEmpleado(int id, Empleado empleado)
        {
            if (id != empleado.Id) return NotFound();

            var existing = await _context.Empleados.FindAsync(id);
            if (existing == null) return NotFound();

            if (ModelState.IsValid)
            {
                string oldEmail = existing.Email;
                existing.Nombre = empleado.Nombre;
                existing.Email = empleado.Email;
                existing.Password = empleado.Password;
                existing.Telefono = empleado.Telefono;
                existing.Cargo = empleado.Cargo;
                existing.Activo = empleado.Activo;
                _context.Empleados.Update(existing);
                await _context.SaveChangesAsync();

                // Actualizar credenciales en Identity
                var identityUser = await _userManager.FindByEmailAsync(oldEmail) ?? await _userManager.FindByEmailAsync(empleado.Email);
                if (identityUser != null)
                {
                    identityUser.Email = empleado.Email;
                    identityUser.UserName = empleado.Email;
                    identityUser.FullName = empleado.Nombre;
                    await _userManager.UpdateAsync(identityUser);

                    if (!string.IsNullOrEmpty(empleado.Password))
                    {
                        var token = await _userManager.GeneratePasswordResetTokenAsync(identityUser);
                        await _userManager.ResetPasswordAsync(identityUser, token, empleado.Password);
                    }

                    if (!await _userManager.IsInRoleAsync(identityUser, "Empleado"))
                    {
                        await _userManager.AddToRoleAsync(identityUser, "Empleado");
                    }
                }
                else
                {
                    identityUser = new ApplicationUser
                    {
                        UserName = empleado.Email,
                        Email = empleado.Email,
                        FullName = empleado.Nombre,
                        EmailConfirmed = true
                    };
                    var createRes = await _userManager.CreateAsync(identityUser, empleado.Password);
                    if (createRes.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(identityUser, "Empleado");
                    }
                }

                TempData["Exito"] = $"Empleado '{empleado.Nombre}' y credenciales actualizadas correctamente.";
                return RedirectToAction(nameof(Empleados));
            }
            return View(empleado);
        }

        public async Task<IActionResult> EliminarEmpleado(int id)
        {
            var emp = await _context.Empleados.FindAsync(id);
            if (emp == null) return NotFound();
            return View(emp);
        }

        [HttpPost, ActionName("EliminarEmpleado")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarEmpleadoConfirmado(int id)
        {
            var emp = await _context.Empleados.FindAsync(id);
            if (emp != null)
            {
                _context.Empleados.Remove(emp);
                await _context.SaveChangesAsync();
                TempData["Exito"] = "Empleado eliminado correctamente.";
            }
            return RedirectToAction(nameof(Empleados));
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
                return RedirectToAction(nameof(HistorialCompras));
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
                return RedirectToAction(nameof(HistorialCompras));
            }

            byte[] excelBytes = _facturaService.GenerarFacturaExcel(pedido);
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Factura_Pedido_{id}.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> DescargarReporteMensual()
        {
            var hoy = DateTime.UtcNow;
            var mesActual = new DateTime(hoy.Year, hoy.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var finDeMes = mesActual.AddMonths(1);

            var pedidosMes = await _context.Pedidos
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Product)
                .Where(p => p.FechaPedido >= mesActual && p.FechaPedido < finDeMes && p.Estado == "Completado")
                .ToListAsync();

            byte[] pdfBytes = _reporteService.GenerarReporteMensualPdf(pedidosMes, hoy);
            return File(pdfBytes, "application/pdf", $"Reporte_Mensual_{hoy:MM_yyyy}.pdf");
        }
    }
}
