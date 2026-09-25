using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using proyecto_asp.Data;
using proyecto_asp.Models;

namespace proyecto_asp.Controllers
{
    public class OpinionesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OpinionesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enviar(int calificacion, string comentario, string nombrePublico)
        {
            if (calificacion < 1 || calificacion > 5)
            {
                TempData["OpinionError"] = "Debes seleccionar entre 1 y 5 estrellas.";
                return RedirectToAction("Index", "Products");
            }

            if (string.IsNullOrWhiteSpace(comentario) || comentario.Length < 5)
            {
                TempData["OpinionError"] = "El comentario debe tener al menos 5 caracteres.";
                return RedirectToAction("Index", "Products");
            }

            var userId = _userManager.GetUserId(User);

            // Si el usuario está logueado usamos su nombre, si no usamos el nombre público ingresado
            string nombre = nombrePublico?.Trim() ?? "Anónimo";
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user?.FullName != null) nombre = user.FullName;
            }

            var opinion = new Opinion
            {
                UserId = userId,
                Calificacion = calificacion,
                Comentario = comentario.Trim(),
                Fecha = DateTime.UtcNow,
                NombrePublico = nombre
            };

            _context.Opiniones.Add(opinion);
            await _context.SaveChangesAsync();

            TempData["OpinionExito"] = "¡Gracias por tu comentario!";
            return RedirectToAction("Index", "Products", null, "testimonios");
        }
    }
}
