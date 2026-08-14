using ClinicaOdontologica.Data;
using ClinicaOdontologica.Models;
using ClinicaOdontologica.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaOdontologica.Controllers;

[Authorize]
public class PaymentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFeeCalculationService _feeCalculationService;

    public PaymentsController(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        IFeeCalculationService feeCalculationService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _feeCalculationService = feeCalculationService;
    }

    // GET: Payments
    public async Task<IActionResult> Index()
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        var isAdmin = _currentUserService.IsAdmin();

        IQueryable<Payment> query = _context.Payments
            .Include(p => p.Unit)
            .Include(p => p.PaymentMethod)
            .Include(p => p.CardBrand);

        // Se não for Admin, filtrar apenas pagamentos do usuário logado
        if (!isAdmin)
        {
            query = query.Where(p => p.CreatedBy == currentUserId);
        }

        return View(await query.OrderByDescending(p => p.CreatedAt).ToListAsync());
    }

    // GET: Payments/Create
    public IActionResult Create()
    {
        ViewBag.PaymentMethods = _context.PaymentMethods.Where(p => p.Active).ToList();
        ViewBag.CardBrands = _context.CardBrands.Where(c => c.Active).ToList();
        return View();
    }

    // POST: Payments/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Payment payment)
    {
        var currentUnitId = _currentUserService.GetCurrentUnitId();
        var currentUserId = _currentUserService.GetCurrentUserId();

        // Validações
        if (payment.GrossAmount <= 0)
        {
            ModelState.AddModelError("GrossAmount", "Valor deve ser maior que zero.");
        }

        var paymentMethod = await _context.PaymentMethods.FindAsync(payment.PaymentMethodId);
        if (paymentMethod == null)
        {
            ModelState.AddModelError("PaymentMethodId", "Forma de pagamento inválida.");
        }
        else
        {
            if (paymentMethod.RequiresBrand && !payment.CardBrandId.HasValue)
            {
                ModelState.AddModelError("CardBrandId", "Bandeira é obrigatória para esta forma de pagamento.");
            }

            if (paymentMethod.RequiresInstallments && !payment.Installments.HasValue)
            {
                ModelState.AddModelError("Installments", "Número de parcelas é obrigatório para esta forma de pagamento.");
            }

            if (paymentMethod.RequiresInstallments && payment.Installments.HasValue && payment.Installments.Value < 1)
            {
                ModelState.AddModelError("Installments", "Número de parcelas deve ser pelo menos 1.");
            }
        }

        if (!ModelState.IsValid)
        {
            ViewBag.PaymentMethods = _context.PaymentMethods.Where(p => p.Active).ToList();
            ViewBag.CardBrands = _context.CardBrands.Where(c => c.Active).ToList();
            return View(payment);
        }

        // Calcular taxa
        try
        {
            var (feePercentage, feeAmount, netAmount) = _feeCalculationService.Calculate(
                payment.PaymentMethodId,
                payment.CardBrandId,
                payment.Installments,
                payment.GrossAmount);

            payment.FeePercentageApplied = feePercentage;
            payment.FeeAmount = feeAmount;
            payment.NetAmountExpected = netAmount;
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.PaymentMethods = _context.PaymentMethods.Where(p => p.Active).ToList();
            ViewBag.CardBrands = _context.CardBrands.Where(c => c.Active).ToList();
            return View(payment);
        }

        // Definir unit_id do usuário logado (nunca do formulário)
        if (currentUnitId.HasValue)
        {
            payment.UnitId = currentUnitId.Value;
        }
        else
        {
            ModelState.AddModelError("", "Usuário não está vinculado a uma unidade.");
            ViewBag.PaymentMethods = _context.PaymentMethods.Where(p => p.Active).ToList();
            ViewBag.CardBrands = _context.CardBrands.Where(c => c.Active).ToList();
            return View(payment);
        }

        payment.Status = PaymentStatus.Pendente;
        payment.CreatedBy = currentUserId;
        payment.CreatedAt = DateTime.UtcNow;

        // Converter PaymentDate para UTC
        if (payment.PaymentDate.Kind == DateTimeKind.Unspecified)
        {
            payment.PaymentDate = DateTime.SpecifyKind(payment.PaymentDate, DateTimeKind.Utc);
        }

        _context.Add(payment);
        await _context.SaveChangesAsync();

        // Criar log de criação
        var log = new PaymentLog
        {
            PaymentId = payment.Id,
            Action = "Created",
            ChangedBy = currentUserId,
            ChangedAt = DateTime.UtcNow
        };
        _context.PaymentLogs.Add(log);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: Payments/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var payment = await _context.Payments.FindAsync(id);
        if (payment == null)
        {
            return NotFound();
        }

        // Só permite edição se status = Pendente ou se for Admin
        var currentUserId = _currentUserService.GetCurrentUserId();
        var isAdmin = _currentUserService.IsAdmin();

        if (payment.Status != PaymentStatus.Pendente && !isAdmin)
        {
            return RedirectToAction(nameof(Index));
        }

        ViewBag.PaymentMethods = _context.PaymentMethods.Where(p => p.Active).ToList();
        ViewBag.CardBrands = _context.CardBrands.Where(c => c.Active).ToList();
        return View(payment);
    }

    // POST: Payments/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Payment payment)
    {
        if (id != payment.Id)
        {
            return NotFound();
        }

        var existingPayment = await _context.Payments.FindAsync(id);
        if (existingPayment == null)
        {
            return NotFound();
        }

        var currentUserId = _currentUserService.GetCurrentUserId();

        // Só permite edição se status = Pendente ou se for Admin
        var isAdmin = _currentUserService.IsAdmin();
        if (existingPayment.Status != PaymentStatus.Pendente && !isAdmin)
        {
            return RedirectToAction(nameof(Index));
        }

        // Validações
        if (payment.GrossAmount <= 0)
        {
            ModelState.AddModelError("GrossAmount", "Valor deve ser maior que zero.");
        }

        var paymentMethod = await _context.PaymentMethods.FindAsync(payment.PaymentMethodId);
        if (paymentMethod == null)
        {
            ModelState.AddModelError("PaymentMethodId", "Forma de pagamento inválida.");
        }
        else
        {
            if (paymentMethod.RequiresBrand && !payment.CardBrandId.HasValue)
            {
                ModelState.AddModelError("CardBrandId", "Bandeira é obrigatória para esta forma de pagamento.");
            }

            if (paymentMethod.RequiresInstallments && !payment.Installments.HasValue)
            {
                ModelState.AddModelError("Installments", "Número de parcelas é obrigatório para esta forma de pagamento.");
            }

            if (paymentMethod.RequiresInstallments && payment.Installments.HasValue && payment.Installments.Value < 1)
            {
                ModelState.AddModelError("Installments", "Número de parcelas deve ser pelo menos 1.");
            }
        }

        if (!ModelState.IsValid)
        {
            ViewBag.PaymentMethods = _context.PaymentMethods.Where(p => p.Active).ToList();
            ViewBag.CardBrands = _context.CardBrands.Where(c => c.Active).ToList();
            return View(payment);
        }

        // Recalcular taxa
        try
        {
            var (feePercentage, feeAmount, netAmount) = _feeCalculationService.Calculate(
                payment.PaymentMethodId,
                payment.CardBrandId,
                payment.Installments,
                payment.GrossAmount);

            existingPayment.FeePercentageApplied = feePercentage;
            existingPayment.FeeAmount = feeAmount;
            existingPayment.NetAmountExpected = netAmount;
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.PaymentMethods = _context.PaymentMethods.Where(p => p.Active).ToList();
            ViewBag.CardBrands = _context.CardBrands.Where(c => c.Active).ToList();
            return View(payment);
        }

        // Atualizar campos
        existingPayment.PatientCode = payment.PatientCode;
        existingPayment.PaymentDate = payment.PaymentDate;
        existingPayment.GrossAmount = payment.GrossAmount;
        existingPayment.PaymentMethodId = payment.PaymentMethodId;
        existingPayment.CardBrandId = payment.CardBrandId;
        existingPayment.Installments = payment.Installments;

        // Converter PaymentDate para UTC se necessário
        if (existingPayment.PaymentDate.Kind == DateTimeKind.Unspecified)
        {
            existingPayment.PaymentDate = DateTime.SpecifyKind(existingPayment.PaymentDate, DateTimeKind.Utc);
        }

        await _context.SaveChangesAsync();

        // Criar log de edição
        var log = new PaymentLog
        {
            PaymentId = existingPayment.Id,
            Action = "Edited",
            ChangedBy = currentUserId,
            ChangedAt = DateTime.UtcNow
        };
        _context.PaymentLogs.Add(log);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
