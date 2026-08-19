using System.Diagnostics;
using ClinicaOdontologica.Data;
using ClinicaOdontologica.Models;
using ClinicaOdontologica.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaOdontologica.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public HomeController(ApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var today = DateTime.Now.Date;
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
}
