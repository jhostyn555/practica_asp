using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proyecto_asp.Data;
using proyecto_asp.Extensions;
using proyecto_asp.Models;

namespace proyecto_asp.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const string CartSessionKey = "CartSession";

        public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private List<CartItem> GetCart()
        {
            return HttpContext.Session.Get<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.Set(CartSessionKey, cart);
        }

        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);

            if (item != null)
            {
                item.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = quantity,
                    ImageUrl = product.ImageUrl
                });
            }

            SaveCart(cart);
            return RedirectToAction("Index", "Products");
        }

        [HttpPost]
        public IActionResult Remove(int productId)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        public IActionResult Checkout()
        {
            var cart = GetCart();
            if (!cart.Any()) return RedirectToAction(nameof(Index));
            return View(cart);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCheckout(string metodoPago)
        {
            var cart = GetCart();
            if (!cart.Any()) return RedirectToAction(nameof(Index));

            var userId = _userManager.GetUserId(User);

            var pedido = new Pedido
            {
                UserId = userId,
                FechaPedido = DateTime.UtcNow,
                Total = cart.Sum(c => c.Total),
                MetodoPago = string.IsNullOrEmpty(metodoPago) ? "Efectivo" : metodoPago,
                Estado = "Pendiente"
            };

            foreach (var item in cart)
            {
                pedido.Detalles.Add(new DetallePedido
                {
                    ProductId = item.ProductId,
                    Cantidad = item.Quantity,
                    PrecioUnitario = item.Price
                });
            }

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            // Descontar stock de cada producto vendido
            foreach (var item in cart)
            {
                var producto = await _context.Products.FindAsync(item.ProductId);
                if (producto != null)
                {
                    producto.Stock -= item.Quantity;
                    _context.Products.Update(producto);
                }
            }

            // Registrar el movimiento financiero de ingreso
            var ingreso = new MovimientoFinanciero
            {
                Tipo = "Ingreso",
                Descripcion = $"Venta online — Pedido #{pedido.Id}",
                Monto = pedido.Total,
                Fecha = DateTime.UtcNow,
                ReferenciaId = pedido.Id,
                EntidadReferencia = "Pedido"
            };
            _context.MovimientosFinancieros.Add(ingreso);
            await _context.SaveChangesAsync();

            // Limpiar carrito
            HttpContext.Session.Remove(CartSessionKey);

            return RedirectToAction("OrderConfirmation", new { id = pedido.Id });
        }

        [Authorize]
        public async Task<IActionResult> OrderConfirmation(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == _userManager.GetUserId(User));

            if (pedido == null) return NotFound();

            return View(pedido);
        }
        
        [HttpGet]
        public IActionResult GetCartCount()
        {
            var cart = GetCart();
            return Json(new { count = cart.Sum(c => c.Quantity) });
        }
    }
}
