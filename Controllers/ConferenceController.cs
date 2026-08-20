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
    private readonly IPdfReportService _pdfReportService;

    public ConferenceController(ApplicationDbContext context, ICurrentUserService currentUserService, IPdfReportService pdfReportService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _pdfReportService = pdfReportService;
    }

    // Helper para obter a data atual no fuso horário brasileiro (UTC-3)
    private DateTime GetBrazilianToday()
    {
        return DateTime.UtcNow.AddHours(-3).Date;
    }

    // GET: Conference
    public async Task<IActionResult> Index(DateTime? startDate = null, DateTime? endDate = null, int? unitId = null, PaymentStatus? status = null, string view = "consolidated")
    {
        // Se não informou data inicial, usa hoje
        var targetStartDate = startDate ?? GetBrazilianToday();
        var targetEndDate = endDate ?? targetStartDate;

        // Converter datas do frontend para UTC
        if (startDate.HasValue && startDate.Value.Kind == DateTimeKind.Unspecified)
        {
            targetStartDate = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
        }
        if (endDate.HasValue && endDate.Value.Kind == DateTimeKind.Unspecified)
        {
            targetEndDate = DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc);
        }

        IQueryable<Payment> query = _context.Payments
            .Include(p => p.Unit)
            .Include(p => p.PaymentMethod)
            .Include(p => p.CardBrand)
            .Where(p => p.PaymentDate.Date >= targetStartDate.Date && p.PaymentDate.Date <= targetEndDate.Date);

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

        // Agrupar por forma de pagamento para visão consolidada
        var consolidated = payments
            .GroupBy(p => new { p.PaymentMethodId, PaymentMethodName = p.PaymentMethod?.Name ?? "Sem Forma" })
            .Select(g => new ConferenceConsolidatedViewModel
            {
                PaymentMethodId = g.Key.PaymentMethodId,
                PaymentMethodName = g.Key.PaymentMethodName,
                Count = g.Count(),
                GrossAmount = g.Sum(p => p.GrossAmount),
                NetAmount = g.Sum(p => p.NetAmountExpected),
                PendingCount = g.Count(p => p.Status == PaymentStatus.Pendente),
                ConfirmedCount = g.Count(p => p.Status == PaymentStatus.OK),
                PendingAmount = g.Where(p => p.Status == PaymentStatus.Pendente).Sum(p => p.NetAmountExpected),
                ConfirmedAmount = g.Where(p => p.Status == PaymentStatus.OK).Sum(p => p.NetAmountExpected)
            })
            .OrderBy(g => g.PaymentMethodName)
            .ToList();

        ViewBag.StartDate = targetStartDate;
        ViewBag.EndDate = targetEndDate;
        ViewBag.SelectedUnitId = unitId;
        ViewBag.SelectedStatus = status;
        ViewBag.Units = units;
        ViewBag.CurrentView = view;
        ViewBag.ConsolidatedData = consolidated;

        if (view == "detailed")
        {
            return View("Detailed", payments);
        }

        return View("Consolidated", consolidated);
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

    // POST: Conference/ConfirmConsolidated
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmConsolidated(int paymentMethodId, DateTime? startDate = null, DateTime? endDate = null, int? unitId = null, PaymentStatus? status = null)
    {
        var targetStartDate = startDate ?? GetBrazilianToday();
        var targetEndDate = endDate ?? targetStartDate;

        // Converter datas do frontend para UTC
        if (startDate.HasValue && startDate.Value.Kind == DateTimeKind.Unspecified)
        {
            targetStartDate = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
        }
        if (endDate.HasValue && endDate.Value.Kind == DateTimeKind.Unspecified)
        {
            targetEndDate = DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc);
        }

        var currentUserId = _currentUserService.GetCurrentUserId();
        var currentUserEmail = User.Identity?.Name ?? currentUserId;

        var payments = await _context.Payments
            .Where(p => p.PaymentMethodId == paymentMethodId 
                && p.PaymentDate.Date >= targetStartDate.Date 
                && p.PaymentDate.Date <= targetEndDate.Date
                && p.Status == PaymentStatus.Pendente)
            .ToListAsync();

        foreach (var payment in payments)
        {
            // Aplicar filtro de unidade se selecionado
            if (unitId.HasValue && payment.UnitId != unitId.Value)
            {
                continue;
            }

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
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { startDate, endDate, unitId, status });
    }

    // POST: Conference/UnconfirmConsolidated
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnconfirmConsolidated(int paymentMethodId, DateTime? startDate = null, DateTime? endDate = null, int? unitId = null, PaymentStatus? status = null)
    {
        var targetStartDate = startDate ?? GetBrazilianToday();
        var targetEndDate = endDate ?? targetStartDate;

        // Converter datas do frontend para UTC
        if (startDate.HasValue && startDate.Value.Kind == DateTimeKind.Unspecified)
        {
            targetStartDate = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
        }
        if (endDate.HasValue && endDate.Value.Kind == DateTimeKind.Unspecified)
        {
            targetEndDate = DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc);
        }

        var currentUserId = _currentUserService.GetCurrentUserId();
        var currentUserEmail = User.Identity?.Name ?? currentUserId;

        var payments = await _context.Payments
            .Where(p => p.PaymentMethodId == paymentMethodId 
                && p.PaymentDate.Date >= targetStartDate.Date 
                && p.PaymentDate.Date <= targetEndDate.Date
                && p.Status == PaymentStatus.OK)
            .ToListAsync();

        foreach (var payment in payments)
        {
            // Aplicar filtro de unidade se selecionado
            if (unitId.HasValue && payment.UnitId != unitId.Value)
            {
                continue;
            }

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
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { startDate, endDate, unitId, status });
    }

    // GET: Conference/GeneratePdf
    public IActionResult GeneratePdf(DateTime? startDate = null, DateTime? endDate = null, int? unitId = null)
    {
        try
        {
            var targetStartDate = startDate ?? GetBrazilianToday();
            var targetEndDate = endDate ?? targetStartDate;

            // Converter datas do frontend para UTC
            if (startDate.HasValue && startDate.Value.Kind == DateTimeKind.Unspecified)
            {
                targetStartDate = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
            }
            if (endDate.HasValue && endDate.Value.Kind == DateTimeKind.Unspecified)
            {
                targetEndDate = DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc);
            }

            var txtBytes = _pdfReportService.GenerateDailyReport(targetStartDate, targetEndDate, unitId);
            
            return File(txtBytes, "text/plain", $"Conciliacao_{targetStartDate:yyyyMMdd}_ate_{targetEndDate:yyyyMMdd}.txt");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Erro ao gerar TXT: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }
}
