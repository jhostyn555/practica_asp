using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using proyecto_asp.Models;

namespace proyecto_asp.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.Migrate();

            // Verificar si ya existen productos para no duplicar excesivamente
            if (!context.Products.Any())
            {
                var products = new Product[]
                {
                    new Product
                    {
                        Name = "Empanada de Queso",
                        Description = "Deliciosa empanada artesanal rellena de abundante queso derretido con masa dorada, crocante y suave al paladar.",
                        Price = 7.00m,
                        Stock = 50,
                        Category = "Empanadas",
                        ImageUrl = "https://encrypted-tbn1.gstatic.com/images?q=tbn:ANd9GcQFPO2APctr4m7I2AmIaypqvCzz4pXhfRdxo4QKocUf6MsJCtLR",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Product
                    {
                        Name = "Empanada Picante",
                        Description = "Empanada tradicional con jugosa carne de res sazonada con ají criollo especial, cebolla caramelizada y toque picante inolvidable.",
                        Price = 8.50m,
                        Stock = 40,
                        Category = "Empanadas",
                        ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQFLVwWoouvxQ81j-92MZ420iARe_APHsulykW1iBFDww&s=10",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Product
                    {
                        Name = "Empanada de Charque",
                        Description = "Empanada gourmet rellena de jugoso charque desmenuzado, queso criollo de finca y la receta secreta exclusiva de Pamelita.",
                        Price = 10.00m,
                        Stock = 30,
                        Category = "Empanadas Especiales",
                        ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSvqrxkioVAlq_mpd3eYAtmVYhnTVCMekYQqzyBQQ2Xcw&s",
                        CreatedAt = DateTime.UtcNow
                    }
                };

                context.Products.AddRange(products);
                context.SaveChanges();
            }
            else
            {
                // Actualizar las URLs de imágenes si ya existen productos con esos nombres
                var queso = context.Products.FirstOrDefault(p => p.Name.Contains("Queso"));
                if (queso != null)
                {
                    queso.ImageUrl = "https://encrypted-tbn1.gstatic.com/images?q=tbn:ANd9GcQFPO2APctr4m7I2AmIaypqvCzz4pXhfRdxo4QKocUf6MsJCtLR";
                }

                var picante = context.Products.FirstOrDefault(p => p.Name.Contains("Picante"));
                if (picante != null)
                {
                    picante.ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQFLVwWoouvxQ81j-92MZ420iARe_APHsulykW1iBFDww&s=10";
                }

                var charque = context.Products.FirstOrDefault(p => p.Name.Contains("Charque"));
                if (charque != null)
                {
                    charque.ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSvqrxkioVAlq_mpd3eYAtmVYhnTVCMekYQqzyBQQ2Xcw&s";
                }

                context.SaveChanges();
            }
        }
    }
}
