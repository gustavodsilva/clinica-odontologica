using ClinicaOdontologica.Data;
using ClinicaOdontologica.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaOdontologica.Controllers;

[Authorize(Roles = "Admin")]
public class PaymentLogsController : Controller
{
    private readonly ApplicationDbContext _context;

    public PaymentLogsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: PaymentLogs
    public async Task<IActionResult> Index(int? paymentId = null)
    {
        IQueryable<PaymentLog> query = _context.PaymentLogs
            .Include(l => l.Payment)
            .ThenInclude(p => p!.Unit)
            .OrderByDescending(l => l.ChangedAt);

        if (paymentId.HasValue)
        {
            query = query.Where(l => l.PaymentId == paymentId.Value);
        }

        var logs = await query.ToListAsync();
        return View(logs);
    }
}
