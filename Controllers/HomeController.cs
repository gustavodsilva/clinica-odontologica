using System.Diagnostics;
using ClinicaOdontologica.Data;
using ClinicaOdontologica.Models;
using ClinicaOdontologica.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaOdontologica.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public HomeController(ApplicationDbContext context, ICurrentUserService currentUserService, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _currentUserService = currentUserService;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var today = DateTime.UtcNow.Date;
            var isAdmin = _currentUserService.IsAdmin();
            var currentUnitId = _currentUserService.GetCurrentUnitId();

            IQueryable<Payment> query = _context.Payments.Where(p => p.PaymentDate.Date == today);

            // Se não for Admin, filtrar apenas pagamentos da unidade do usuário
            if (!isAdmin && currentUnitId.HasValue)
            {
                query = query.Where(p => p.UnitId == currentUnitId.Value);
            }

            var payments = await query.ToListAsync();

            var model = new HomeViewModel
            {
                TodayDate = today,
                TotalPayments = payments.Count,
                PendingPayments = payments.Count(p => p.Status == PaymentStatus.Pendente),
                ConfirmedPayments = payments.Count(p => p.Status == PaymentStatus.OK),
                TotalGrossAmount = payments.Sum(p => p.GrossAmount)
            };

            // Se for Admin, calcular resumo por unidade
            if (isAdmin)
            {
                var paymentsByUnit = await _context.Payments
                    .Where(p => p.PaymentDate.Date == today)
                    .Include(p => p.Unit)
                    .GroupBy(p => p.UnitId)
                    .Select(g => new PaymentByUnitSummary
                    {
                        UnitName = g.FirstOrDefault()!.Unit!.Name,
                        Total = g.Count(),
                        Pending = g.Count(p => p.Status == PaymentStatus.Pendente),
                        Confirmed = g.Count(p => p.Status == PaymentStatus.OK),
                        GrossAmount = g.Sum(p => p.GrossAmount)
                    })
                    .ToListAsync();

                model.PaymentsByUnit = paymentsByUnit;
            }

            return View(model);
        }
        return RedirectToAction("Login", "Account");
    }

    [Authorize]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    // Temporary endpoint to initialize database (remove after first use in production)
    public async Task<IActionResult> SeedDatabase()
    {
        try
        {
            await DbInitializer.Initialize(_context, _userManager, _roleManager);
            return Content("Database seeded successfully! Please remove this endpoint after first use.");
        }
        catch (Exception ex)
        {
            return Content($"Error seeding database: {ex.Message}");
        }
    }
}
