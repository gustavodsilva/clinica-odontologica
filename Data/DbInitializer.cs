using ClinicaOdontologica.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicaOdontologica.Data;

public static class DbInitializer
{
    public static async Task Initialize(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        // Garantir que o banco foi criado
        context.Database.EnsureCreated();

        // Criar roles se não existirem
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        if (!await roleManager.RoleExistsAsync("Recepcao"))
        {
            await roleManager.CreateAsync(new IdentityRole("Recepcao"));
        }

        // Criar usuário Admin se não existir
        var adminUser = await userManager.FindByEmailAsync("admin@clinica.com");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin@clinica.com",
                Email = "admin@clinica.com",
                UnitId = null // Admin tem acesso a todas as unidades
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
}
