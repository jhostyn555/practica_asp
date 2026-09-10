using Microsoft.AspNetCore.Mvc;
using proyecto_asp.Data;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        try
        {
            // Intenta realizar una consulta ligera
            bool canConnect = _context.Database.CanConnect();

            if (canConnect)
            {
                // Consulta los productos para confirmar la lectura de datos
                var productos = _context.Products.ToList();
                ViewBag.Status = $"Conexión exitosa. Se encontraron {productos.Count} productos.";
            }
            else
            {
                ViewBag.Status = "No se pudo conectar a la base de datos.";
            }
        }
        catch (Exception ex)
        {
            ViewBag.Status = $"Error de conexión: {ex.Message}";
        }

        return View();
    }
}