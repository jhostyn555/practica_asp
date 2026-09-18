using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proyecto_asp.Data;
using proyecto_asp.Models;

namespace proyecto_asp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class InsumosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InsumosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Insumos/Index
        public async Task<IActionResult> Index()
        {
            var insumos = await _context.Insumos.OrderBy(i => i.Id).ToListAsync();
            return View(insumos);
        }

        // GET: /Insumos/Crear
        public IActionResult Crear()
        {
            return View();
        }

        // POST: /Insumos/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Insumo insumo)
        {
            if (ModelState.IsValid)
            {
                insumo.CreatedAt = DateTime.UtcNow;
                _context.Insumos.Add(insumo);
                await _context.SaveChangesAsync();
                TempData["Exito"] = $"Insumo '{insumo.Nombre}' creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(insumo);
        }

        // GET: /Insumos/Editar/5
        public async Task<IActionResult> Editar(int id)
        {
            var insumo = await _context.Insumos.FindAsync(id);
            if (insumo == null) return NotFound();
            return View(insumo);
        }

        // POST: /Insumos/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Insumo insumo)
        {
            if (id != insumo.Id) return NotFound();

            if (ModelState.IsValid)
            {
                insumo.UpdatedAt = DateTime.UtcNow;
                _context.Insumos.Update(insumo);
                await _context.SaveChangesAsync();
                TempData["Exito"] = $"Insumo '{insumo.Nombre}' actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(insumo);
        }

        // GET: /Insumos/Eliminar/5
        public async Task<IActionResult> Eliminar(int id)
        {
            var insumo = await _context.Insumos.FindAsync(id);
            if (insumo == null) return NotFound();
            return View(insumo);
        }

        // POST: /Insumos/Eliminar/5
        [HttpPost, ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarConfirmado(int id)
        {
            var insumo = await _context.Insumos.FindAsync(id);
            if (insumo != null)
            {
                _context.Insumos.Remove(insumo);
                await _context.SaveChangesAsync();
                TempData["Exito"] = $"Insumo eliminado correctamente.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
