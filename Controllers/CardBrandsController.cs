using ClinicaOdontologica.Data;
using ClinicaOdontologica.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaOdontologica.Controllers;

[Authorize(Roles = "Admin")]
public class CardBrandsController : Controller
{
    private readonly ApplicationDbContext _context;

    public CardBrandsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: CardBrands
    public async Task<IActionResult> Index()
    {
        return View(await _context.CardBrands.ToListAsync());
    }

    // GET: CardBrands/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: CardBrands/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CardBrand cardBrand)
    {
        if (ModelState.IsValid)
        {
            _context.Add(cardBrand);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(cardBrand);
    }

    // GET: CardBrands/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var cardBrand = await _context.CardBrands.FindAsync(id);
        if (cardBrand == null)
        {
            return NotFound();
        }
        return View(cardBrand);
    }

    // POST: CardBrands/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CardBrand cardBrand)
    {
        if (id != cardBrand.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(cardBrand);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CardBrandExists(cardBrand.Id))
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
        return View(cardBrand);
    }

    // GET: CardBrands/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var cardBrand = await _context.CardBrands.FirstOrDefaultAsync(m => m.Id == id);
        if (cardBrand == null)
        {
            return NotFound();
        }

        return View(cardBrand);
    }

    // POST: CardBrands/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var cardBrand = await _context.CardBrands.FindAsync(id);
        if (cardBrand != null)
        {
            _context.CardBrands.Remove(cardBrand);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private bool CardBrandExists(int id)
    {
        return _context.CardBrands.Any(e => e.Id == id);
    }
}
