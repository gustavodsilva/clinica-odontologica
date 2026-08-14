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

        // Seed de unidades se não existirem
        await SeedUnits(context);

        // Seed de formas de pagamento se não existirem
        await SeedPaymentMethods(context);

        // Seed de bandeiras se não existirem
        await SeedCardBrands(context);

        // Seed de usuários se não existirem
        await SeedUsers(context, userManager, roleManager);

        // Seed de regras de taxa se não existirem
        await SeedFeeRules(context);
    }

    private static async Task SeedUnits(ApplicationDbContext context)
    {
        if (await context.Units.AnyAsync())
        {
            return; // Já foram criadas, não recriar
        }

        var units = new List<Unit>
        {
            new Unit { Name = "Palmeiras - Suzano", Active = true },
            new Unit { Name = "Ferraz de Vasconcelos", Active = true }
        };

        await context.Units.AddRangeAsync(units);
        await context.SaveChangesAsync();
    }

    private static async Task SeedPaymentMethods(ApplicationDbContext context)
    {
        if (await context.PaymentMethods.AnyAsync())
        {
            return; // Já foram criadas, não recriar
        }

        var paymentMethods = new List<PaymentMethod>
        {
            new PaymentMethod { Name = "Dinheiro", Active = true, RequiresBrand = false, RequiresInstallments = false },
            new PaymentMethod { Name = "Pix", Active = true, RequiresBrand = false, RequiresInstallments = false },
            new PaymentMethod { Name = "Boleto", Active = true, RequiresBrand = false, RequiresInstallments = false },
            new PaymentMethod { Name = "Cartão de Débito", Active = true, RequiresBrand = true, RequiresInstallments = false },
            new PaymentMethod { Name = "Cartão de Crédito", Active = true, RequiresBrand = true, RequiresInstallments = true }
        };

        await context.PaymentMethods.AddRangeAsync(paymentMethods);
        await context.SaveChangesAsync();
    }

    private static async Task SeedCardBrands(ApplicationDbContext context)
    {
        if (await context.CardBrands.AnyAsync())
        {
            return; // Já foram criadas, não recriar
        }

        var cardBrands = new List<CardBrand>
        {
            new CardBrand { Name = "Mastercard", Active = true },
            new CardBrand { Name = "Visa", Active = true },
            new CardBrand { Name = "Elo", Active = true },
            new CardBrand { Name = "Amex", Active = true },
            new CardBrand { Name = "Outras Bandeiras", Active = true }
        };

        await context.CardBrands.AddRangeAsync(cardBrands);
        await context.SaveChangesAsync();
    }

    private static async Task SeedUsers(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        // Buscar unidades
        var palmeirasUnit = await context.Units.FirstOrDefaultAsync(u => u.Name == "Palmeiras - Suzano");
        var ferrazUnit = await context.Units.FirstOrDefaultAsync(u => u.Name == "Ferraz de Vasconcelos");

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

        // Criar usuário admin@clinicarisos.com.br se não existir
        var adminRisosUser = await userManager.FindByEmailAsync("admin@clinicarisos.com.br");
        if (adminRisosUser == null)
        {
            adminRisosUser = new ApplicationUser
            {
                UserName = "admin@clinicarisos.com.br",
                Email = "admin@clinicarisos.com.br",
                UnitId = null
            };

            var result = await userManager.CreateAsync(adminRisosUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminRisosUser, "Admin");
            }
        }

        // Criar usuário debora se não existir
        var deboraUser = await userManager.FindByEmailAsync("debora@clinicarisos.com.br");
        if (deboraUser == null && palmeirasUnit != null)
        {
            deboraUser = new ApplicationUser
            {
                UserName = "debora@clinicarisos.com.br",
                Email = "debora@clinicarisos.com.br",
                UnitId = palmeirasUnit.Id
            };

            var result = await userManager.CreateAsync(deboraUser, "Recepcao@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(deboraUser, "Recepcao");
            }
        }

        // Criar usuário beatriz se não existir
        var beatrizUser = await userManager.FindByEmailAsync("beatriz@clinicarisos.com.br");
        if (beatrizUser == null && ferrazUnit != null)
        {
            beatrizUser = new ApplicationUser
            {
                UserName = "beatriz@clinicarisos.com.br",
                Email = "beatriz@clinicarisos.com.br",
                UnitId = ferrazUnit.Id
            };

            var result = await userManager.CreateAsync(beatrizUser, "Recepcao@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(beatrizUser, "Recepcao");
            }
        }

        // Criar usuário richard se não existir
        var richardUser = await userManager.FindByEmailAsync("richard@clinicarisos.com.br");
        if (richardUser == null && ferrazUnit != null)
        {
            richardUser = new ApplicationUser
            {
                UserName = "richard@clinicarisos.com.br",
                Email = "richard@clinicarisos.com.br",
                UnitId = ferrazUnit.Id
            };

            var result = await userManager.CreateAsync(richardUser, "Recepcao@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(richardUser, "Recepcao");
            }
        }
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
