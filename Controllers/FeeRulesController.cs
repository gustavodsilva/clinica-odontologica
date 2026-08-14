using ClinicaOdontologica.Data;
using ClinicaOdontologica.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaOdontologica.Controllers;

[Authorize(Roles = "Admin")]
public class FeeRulesController : Controller
{
    private readonly ApplicationDbContext _context;

    public FeeRulesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: FeeRules
    public async Task<IActionResult> Index()
    {
        var feeRules = await _context.FeeRules
            .Include(f => f.PaymentMethod)
            .Include(f => f.CardBrand)
            .ToListAsync();
        return View(feeRules);
    }

    // GET: FeeRules/Create
    public IActionResult Create()
    {
        ViewBag.PaymentMethods = _context.PaymentMethods.Where(p => p.Active).ToList();
        ViewBag.CardBrands = _context.CardBrands.Where(c => c.Active).ToList();
        return View();
    }

    // POST: FeeRules/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FeeRule feeRule)
    {
        if (ModelState.IsValid)
        {
            _context.Add(feeRule);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.PaymentMethods = _context.PaymentMethods.Where(p => p.Active).ToList();
        ViewBag.CardBrands = _context.CardBrands.Where(c => c.Active).ToList();
        return View(feeRule);
    }

    // GET: FeeRules/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var feeRule = await _context.FeeRules.FindAsync(id);
        if (feeRule == null)
        {
            return NotFound();
        }

        ViewBag.PaymentMethods = _context.PaymentMethods.Where(p => p.Active).ToList();
        ViewBag.CardBrands = _context.CardBrands.Where(c => c.Active).ToList();
        return View(feeRule);
    }

    // POST: FeeRules/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FeeRule feeRule)
    {
        if (id != feeRule.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(feeRule);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FeeRuleExists(feeRule.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        ViewBag.PaymentMethods = _context.PaymentMethods.Where(p => p.Active).ToList();
        ViewBag.CardBrands = _context.CardBrands.Where(c => c.Active).ToList();
        return View(feeRule);
    }

    // GET: FeeRules/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var feeRule = await _context.FeeRules
            .Include(f => f.PaymentMethod)
            .Include(f => f.CardBrand)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (feeRule == null)
        {
            return NotFound();
        }

        return View(feeRule);
    }

    // POST: FeeRules/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var feeRule = await _context.FeeRules.FindAsync(id);
        if (feeRule != null)
        {
            _context.FeeRules.Remove(feeRule);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private bool FeeRuleExists(int id)
    {
        return _context.FeeRules.Any(e => e.Id == id);
    }
}
