using proyecto_asp.Data;
using proyecto_asp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace proyecto_asp.Controllers
{
    // Constructor primario: "context" reemplaza el campo _context de antes.
    [Authorize]
    public class ProductsController(ApplicationDbContext context) : Controller
    {
        [AllowAnonymous]
        public async Task<IActionResult> Index(string searchQuery, string categoria)
        {
            var query = context.Products.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(p => p.Name.Contains(searchQuery) || p.Description.Contains(searchQuery));
                ViewBag.SearchQuery = searchQuery;
            }

            if (!string.IsNullOrEmpty(categoria))
            {
                query = query.Where(p => p.Category == categoria);
                ViewBag.Categoria = categoria;
            }

            var products = await query.ToListAsync();

            // Cargar opiniones para la sección de testimonios
            ViewBag.Opiniones = await context.Opiniones
                .Include(o => o.User)
                .OrderByDescending(o => o.Id)
                .Take(20)
                .ToListAsync();

            // Cargar categorías activas para los botones de filtro dinámicos
            ViewBag.Categorias = await context.Categorias
                .Where(c => c.Activa)
                .OrderBy(c => c.Id)
                .ToListAsync();

            return View(products);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var product = await context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Categoria(string nombre, string searchQuery)
        {
            if (string.IsNullOrEmpty(nombre)) return RedirectToAction(nameof(Index));

            var cat = await context.Categorias.FirstOrDefaultAsync(c => c.Nombre == nombre);

            var query = context.Products.AsNoTracking().Where(p => p.Category == nombre);

            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(p => p.Name.Contains(searchQuery) || p.Description.Contains(searchQuery));
                ViewBag.SearchQuery = searchQuery;
            }

            var products = await query.ToListAsync();

            ViewBag.CategoriaNombre = nombre;
            ViewBag.CategoriaIcono = cat?.Icono ?? "bi-tag";

            return View(products);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categorias = await context.Categorias.Where(c => c.Activa).OrderBy(c => c.Nombre).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Product product)
        {
            if (!ModelState.IsValid) return View(product);
            context.Products.Add(product);
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await context.Products.FindAsync(id);
            if (product == null) return NotFound();
            ViewBag.Categorias = await context.Categorias.Where(c => c.Activa).OrderBy(c => c.Nombre).ToListAsync();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id) return NotFound();
            if (!ModelState.IsValid) return View(product);

            product.UpdatedAt = DateTime.UtcNow;
            context.Products.Update(product);
            await context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await context.Products.FindAsync(id);
            if (product != null)
            {
                context.Products.Remove(product);
                await context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
