using ClinicaOdontologica.Data;
using ClinicaOdontologica.Models;
using ClinicaOdontologica.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaOdontologica.Controllers;

[Authorize(Roles = "Admin")]
public class ConferenceController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ConferenceController(ApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    // GET: Conference
    public async Task<IActionResult> Index(DateTime? date = null, int? unitId = null, PaymentStatus? status = null)
    {
        // Se não informou data, usa ontem
        var targetDate = date ?? DateTime.UtcNow.AddDays(-1).Date;

        // Converter para UTC se necessário
        if (targetDate.Kind == DateTimeKind.Unspecified)
        {
            targetDate = DateTime.SpecifyKind(targetDate, DateTimeKind.Utc);
        }

        IQueryable<Payment> query = _context.Payments
            .Include(p => p.Unit)
            .Include(p => p.PaymentMethod)
            .Include(p => p.CardBrand)
            .Where(p => p.PaymentDate.Date == targetDate);

        // Filtro por unidade
        if (unitId.HasValue)
        {
            query = query.Where(p => p.UnitId == unitId.Value);
        }

        // Filtro por status
        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        var payments = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        var units = await _context.Units.Where(u => u.Active).ToListAsync();

        ViewBag.TargetDate = targetDate;
        ViewBag.SelectedUnitId = unitId;
        ViewBag.SelectedStatus = status;
        ViewBag.Units = units;

        return View(payments);
    }

    // POST: Conference/Confirm/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id)
    {
        var payment = await _context.Payments.FindAsync(id);
        if (payment == null)
        {
            return NotFound();
        }

        var currentUserId = _currentUserService.GetCurrentUserId();
        var currentUserEmail = User.Identity?.Name ?? currentUserId;

        // Criar log da alteração
        var log = new PaymentLog
        {
            PaymentId = payment.Id,
            Action = "StatusChanged",
            ChangedBy = currentUserEmail,
            OldValue = payment.Status.ToString(),
            NewValue = PaymentStatus.OK.ToString(),
            ChangedAt = DateTime.UtcNow
        };

        payment.Status = PaymentStatus.OK;
        payment.ConfirmedBy = currentUserId;
        payment.ConfirmedAt = DateTime.UtcNow;

        _context.PaymentLogs.Add(log);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // POST: Conference/Unconfirm/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unconfirm(int id)
    {
        var payment = await _context.Payments.FindAsync(id);
        if (payment == null)
        {
            return NotFound();
        }

        var currentUserId = _currentUserService.GetCurrentUserId();
        var currentUserEmail = User.Identity?.Name ?? currentUserId;

        // Criar log da alteração
        var log = new PaymentLog
        {
            PaymentId = payment.Id,
            Action = "StatusChanged",
            ChangedBy = currentUserEmail,
            OldValue = payment.Status.ToString(),
            NewValue = PaymentStatus.Pendente.ToString(),
            ChangedAt = DateTime.UtcNow
        };

        payment.Status = PaymentStatus.Pendente;
        payment.ConfirmedBy = null;
        payment.ConfirmedAt = null;

        _context.PaymentLogs.Add(log);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
