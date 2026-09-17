using Microsoft.AspNetCore.Mvc;
using proyecto_asp.Data;
using proyecto_asp.Models;

namespace proyecto_asp.Controllers
{
    public class ContactoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContactoController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enviar(Contacto contacto)
        {
            if (ModelState.IsValid)
            {
                contacto.FechaEnvio = DateTime.UtcNow;
                contacto.Leido = false;
                _context.Contactos.Add(contacto);
                await _context.SaveChangesAsync();

                TempData["MensajeExito"] = "¡Gracias por contactarnos! Tu mensaje ha sido enviado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            return View("Index", contacto);
        }
    }
}
