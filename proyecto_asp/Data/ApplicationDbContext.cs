using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using proyecto_asp.Models;

namespace proyecto_asp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<DetallePedido> DetallesPedido { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Compra> Compras { get; set; }
        public DbSet<Opinion> Opiniones { get; set; }
        public DbSet<Contacto> Contactos { get; set; }
        public DbSet<MovimientoFinanciero> MovimientosFinancieros { get; set; }
        public DbSet<Insumo> Insumos { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Empleado> Empleados { get; set; }

        // MÃ©todo estÃ¡tico para traducir consultas LINQ a PostgreSQL
        public static string Unaccent(string text) => throw new NotSupportedException();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Mapeo de la funciÃ³n f_unaccent creada en Supabase
            builder.HasDbFunction(typeof(ApplicationDbContext).GetMethod(nameof(Unaccent))!)
                   .HasName("f_unaccent");
        }
    }
}