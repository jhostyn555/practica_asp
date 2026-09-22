using proyecto_asp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace proyecto_asp.Controllers
{
    // Constructor primario: userManager y signInManager quedan disponibles
    // en toda la clase sin declarar campos ni constructor explícito.
    public class AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager) : Controller
    {
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var user = await userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    if (await userManager.IsInRoleAsync(user, "Admin"))
                    {
                        return RedirectToAction("Dashboard", "Admin");
                    }
                    if (await userManager.IsInRoleAsync(user, "Empleado"))
                    {
                        return RedirectToAction("Index", "Empleado");
                    }
                }
                return RedirectToAction("Index", "Products");
            }

            ModelState.AddModelError(string.Empty, "Credenciales inválidas");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Address = model.Address
            };

            var result = await userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Products");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Products");
        }

        // ==========================================
        // MÉTODOS PARA LOGIN CON GOOGLE (OAUTH)
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            returnUrl ??= Url.Action("Index", "Products");

            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Error del proveedor externo: {remoteError}");
                return View("Login");
            }

            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ModelState.AddModelError(string.Empty, "Error al cargar la información de autenticación externa.");
                return View("Login");
            }

            // 1. Intentar iniciar sesión si la cuenta de Google ya está vinculada en AspNetUserLogins
            var result = await signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: false,
                bypassTwoFactor: true);

            if (result.Succeeded)
            {
                return LocalRedirect(returnUrl);
            }

            // 2. Si el usuario no ha vinculado la cuenta, buscamos por correo
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);

            if (email != null)
            {
                var user = await userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    // Crear el usuario automáticamente en Supabase / Postgres
                    var name = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email;

                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FullName = name,
                        Address = "Registrado con Google",
                        EmailConfirmed = true
                    };

                    var createResult = await userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        foreach (var error in createResult.Errors)
                            ModelState.AddModelError(string.Empty, error.Description);

                        return View("Login");
                    }
                }

                // Vincular el proveedor externo (Google) al usuario local
                var addLoginResult = await userManager.AddLoginAsync(user, info);
                if (addLoginResult.Succeeded)
                {
                    await signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }
            }

            ModelState.AddModelError(string.Empty, "No se pudo iniciar sesión con Google.");
            return View("Login");

            
        }
        [HttpGet]
        public IActionResult AccessDenied() => View();
    }
}