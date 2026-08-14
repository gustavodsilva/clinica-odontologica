using ClinicaOdontologica.Data;
using ClinicaOdontologica.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaOdontologica.Controllers;

[Authorize(Roles = "Admin")]
public class PaymentMethodsController : Controller
{
    private readonly ApplicationDbContext _context;

    public PaymentMethodsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: PaymentMethods
    public async Task<IActionResult> Index()
    {
        return View(await _context.PaymentMethods.ToListAsync());
    }

    // GET: PaymentMethods/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: PaymentMethods/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PaymentMethod paymentMethod)
    {
        if (ModelState.IsValid)
        {
            _context.Add(paymentMethod);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(paymentMethod);
    }

    // GET: PaymentMethods/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var paymentMethod = await _context.PaymentMethods.FindAsync(id);
        if (paymentMethod == null)
        {
            return NotFound();
        }
        return View(paymentMethod);
    }

    // POST: PaymentMethods/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PaymentMethod paymentMethod)
    {
        if (id != paymentMethod.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(paymentMethod);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PaymentMethodExists(paymentMethod.Id))
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
        return View(paymentMethod);
    }

    // GET: PaymentMethods/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var paymentMethod = await _context.PaymentMethods.FirstOrDefaultAsync(m => m.Id == id);
        if (paymentMethod == null)
        {
            return NotFound();
        }

        return View(paymentMethod);
    }

    // POST: PaymentMethods/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var paymentMethod = await _context.PaymentMethods.FindAsync(id);
        if (paymentMethod != null)
        {
            _context.PaymentMethods.Remove(paymentMethod);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private bool PaymentMethodExists(int id)
    {
        return _context.PaymentMethods.Any(e => e.Id == id);
    }
}
