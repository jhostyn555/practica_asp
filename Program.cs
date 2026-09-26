using proyecto_asp.Data;
using proyecto_asp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Configuración de la base de datos PostgreSQL / Supabase
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuración de ASP.NET Core Identity con Roles + Google Authentication
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Integración de inicio de sesión externo con Google para Identity
if (!string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Google:ClientId"]) &&
    !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Google:ClientSecret"]))
{
    builder.Services.AddAuthentication().AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    });
}

// Configuración de Cookies
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<proyecto_asp.Services.FacturaService>();
builder.Services.AddScoped<proyecto_asp.Services.ReporteService>();

builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

var app = builder.Build();

// Soporte para Render (Proxy Inverso para la redirección HTTPS de Google)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // CORS para PWA
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        
        // MIME types correctos para PWA
        var path = ctx.Context.Request.Path.Value?.ToLowerInvariant();
        if (path?.EndsWith(".webmanifest") == true || path?.EndsWith("manifest.json") == true)
        {
            ctx.Context.Response.ContentType = "application/manifest+json";
        }
        else if (path?.EndsWith(".js") == true && path.Contains("sw.js"))
        {
            ctx.Context.Response.ContentType = "application/javascript";
            ctx.Context.Response.Headers.Append("Service-Worker-Allowed", "/");
        }
    }
});

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Products}/{action=Index}/{id?}");

// Inicialización de la base de datos, Migraciones, Roles y Usuario Admin
if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();

        // Aplica todas las migraciones pendientes automáticamente a Supabase
        dbContext.Database.Migrate();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // 1. Crear el rol Administrador y Empleado si no existen
        string roleName = "Admin";
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }

        string empleadoRoleName = "Empleado";
        if (!await roleManager.RoleExistsAsync(empleadoRoleName))
        {
            await roleManager.CreateAsync(new IdentityRole(empleadoRoleName));
        }

        // 2. Datos del nuevo Administrador
        string adminEmail = "admin@pamelita.com";
        string? adminPassword = builder.Configuration["Admin:Password"];

        var user = await userManager.FindByEmailAsync(adminEmail);

        if (user == null && !string.IsNullOrWhiteSpace(adminPassword))
        {
            user = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "Administrador Pamelita",
                Address = "Sucursal Principal"
            };

            var createResult = await userManager.CreateAsync(user, adminPassword);

            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(user, roleName);
            }
        }
        else
        {
            if (!await userManager.IsInRoleAsync(user, roleName))
            {
                await userManager.AddToRoleAsync(user, roleName);
            }
        }

        // Inicializador general si tienes seeders adicionales
        DbInitializer.Initialize(dbContext);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al inicializar la base de datos o el administrador.");
    }
}

app.Run();
