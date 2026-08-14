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

        // Seed de regras de taxa se não existirem
        await SeedFeeRules(context);
    }

    private static async Task SeedFeeRules(ApplicationDbContext context)
    {
        // Verificar se já existem regras de taxa
        if (await context.FeeRules.AnyAsync())
        {
            return; // Já foram criadas, não recriar
        }

        // Buscar formas de pagamento e bandeiras por nome
        var debitCard = await context.PaymentMethods.FirstOrDefaultAsync(pm => pm.Name == "Cartão de Débito");
        var creditCard = await context.PaymentMethods.FirstOrDefaultAsync(pm => pm.Name == "Cartão de Crédito");

        var mastercard = await context.CardBrands.FirstOrDefaultAsync(cb => cb.Name == "Mastercard");
        var visa = await context.CardBrands.FirstOrDefaultAsync(cb => cb.Name == "Visa");
        var elo = await context.CardBrands.FirstOrDefaultAsync(cb => cb.Name == "Elo");
        var amex = await context.CardBrands.FirstOrDefaultAsync(cb => cb.Name == "Amex");
        var outras = await context.CardBrands.FirstOrDefaultAsync(cb => cb.Name == "Outras Bandeiras");

        if (debitCard == null || creditCard == null)
        {
            return; // Formas de pagamento não encontradas
        }

        var feeRules = new List<FeeRule>();

        // Cartão de Débito
        if (mastercard != null) feeRules.Add(new FeeRule { PaymentMethodId = debitCard.Id, CardBrandId = mastercard.Id, Installments = null, FeePercentage = 0.79m, Active = true });
        if (visa != null) feeRules.Add(new FeeRule { PaymentMethodId = debitCard.Id, CardBrandId = visa.Id, Installments = null, FeePercentage = 0.79m, Active = true });
        if (elo != null) feeRules.Add(new FeeRule { PaymentMethodId = debitCard.Id, CardBrandId = elo.Id, Installments = null, FeePercentage = 1.59m, Active = true });
        if (amex != null) feeRules.Add(new FeeRule { PaymentMethodId = debitCard.Id, CardBrandId = amex.Id, Installments = null, FeePercentage = 1.59m, Active = true });
        // Outras Bandeiras para débito não tem taxa

        // Cartão de Crédito - 1 parcela (Rotativo)
        if (mastercard != null) feeRules.Add(new FeeRule { PaymentMethodId = creditCard.Id, CardBrandId = mastercard.Id, Installments = 1, FeePercentage = 2.79m, Active = true });
        if (visa != null) feeRules.Add(new FeeRule { PaymentMethodId = creditCard.Id, CardBrandId = visa.Id, Installments = 1, FeePercentage = 2.79m, Active = true });
        if (elo != null) feeRules.Add(new FeeRule { PaymentMethodId = creditCard.Id, CardBrandId = elo.Id, Installments = 1, FeePercentage = 3.59m, Active = true });
        if (amex != null) feeRules.Add(new FeeRule { PaymentMethodId = creditCard.Id, CardBrandId = amex.Id, Installments = 1, FeePercentage = 3.59m, Active = true });
        if (outras != null) feeRules.Add(new FeeRule { PaymentMethodId = creditCard.Id, CardBrandId = outras.Id, Installments = 1, FeePercentage = 3.59m, Active = true });

        // Cartão de Crédito - Parcelado Mastercard
        if (mastercard != null)
        {
            for (int i = 2; i <= 12; i++)
            {
                decimal fee = 2.79m + (i - 1) * 0.87m; // 2.79 + (n-1)*0.87
                feeRules.Add(new FeeRule { PaymentMethodId = creditCard.Id, CardBrandId = mastercard.Id, Installments = i, FeePercentage = fee, Active = true });
            }
        }

        // Cartão de Crédito - Parcelado Visa (mesma taxa do Mastercard)
        if (visa != null)
        {
            for (int i = 2; i <= 12; i++)
            {
                decimal fee = 2.79m + (i - 1) * 0.87m;
                feeRules.Add(new FeeRule { PaymentMethodId = creditCard.Id, CardBrandId = visa.Id, Installments = i, FeePercentage = fee, Active = true });
            }
        }

        // Cartão de Crédito - Parcelado Elo
        if (elo != null)
        {
            for (int i = 2; i <= 12; i++)
            {
                decimal fee = 3.59m + (i - 1) * 0.87m; // 3.59 + (n-1)*0.87
                feeRules.Add(new FeeRule { PaymentMethodId = creditCard.Id, CardBrandId = elo.Id, Installments = i, FeePercentage = fee, Active = true });
            }
        }

        // Cartão de Crédito - Parcelado Amex (mesma taxa do Elo)
        if (amex != null)
        {
            for (int i = 2; i <= 12; i++)
            {
                decimal fee = 3.59m + (i - 1) * 0.87m;
                feeRules.Add(new FeeRule { PaymentMethodId = creditCard.Id, CardBrandId = amex.Id, Installments = i, FeePercentage = fee, Active = true });
            }
        }

        // Cartão de Crédito - Parcelado Outras Bandeiras (mesma taxa do Elo)
        if (outras != null)
        {
            for (int i = 2; i <= 12; i++)
            {
                decimal fee = 3.59m + (i - 1) * 0.87m;
                feeRules.Add(new FeeRule { PaymentMethodId = creditCard.Id, CardBrandId = outras.Id, Installments = i, FeePercentage = fee, Active = true });
            }
        }

        await context.FeeRules.AddRangeAsync(feeRules);
        await context.SaveChangesAsync();
    }
}
