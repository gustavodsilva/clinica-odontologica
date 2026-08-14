using ClinicaOdontologica.Data;
using ClinicaOdontologica.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaOdontologica.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UsersController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // GET: Users
    public async Task<IActionResult> Index()
    {
        var users = await _context.Users
            .Include(u => u.Unit)
            .ToListAsync();
        
        var userRoles = new Dictionary<string, string>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userRoles[user.Id] = roles.FirstOrDefault() ?? "Sem role";
        }

        ViewBag.UserRoles = userRoles;
        return View(users);
    }

    // GET: Users/Edit/5
    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var user = await _context.Users.Include(u => u.Unit).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var currentRole = roles.FirstOrDefault();

        ViewBag.Units = _context.Units.Where(u => u.Active).ToList();
        ViewBag.CurrentRole = currentRole;

        var editModel = new UserEditViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Role = currentRole ?? string.Empty,
            UnitId = user.UnitId
        };

        return View(editModel);
    }

    // POST: Users/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, UserEditViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (string.IsNullOrEmpty(model.Email))
        {
            ModelState.AddModelError("", "Email é obrigatório.");
            ViewBag.Units = _context.Units.Where(u => u.Active).ToList();
            ViewBag.CurrentRole = model.Role;
            return View(model);
        }

        if (model.Role != "Admin" && model.Role != "Recepcao")
        {
            ModelState.AddModelError("", "Role deve ser Admin ou Recepcao.");
            ViewBag.Units = _context.Units.Where(u => u.Active).ToList();
            ViewBag.CurrentRole = model.Role;
            return View(model);
        }

        if (model.Role == "Recepcao" && !model.UnitId.HasValue)
        {
            ModelState.AddModelError("", "Usuário de Recepcao deve estar vinculado a uma unidade.");
            ViewBag.Units = _context.Units.Where(u => u.Active).ToList();
            ViewBag.CurrentRole = model.Role;
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Atualizar email se mudou
        if (user.Email != model.Email)
        {
            user.Email = model.Email;
            user.UserName = model.Email;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                ViewBag.Units = _context.Units.Where(u => u.Active).ToList();
                ViewBag.CurrentRole = model.Role;
                return View(model);
            }
        }

        // Atualizar unit_id
        user.UnitId = model.Role == "Admin" ? null : model.UnitId;
        await _context.SaveChangesAsync();

        // Atualizar role se mudou
        var currentRoles = await _userManager.GetRolesAsync(user);
        var currentRole = currentRoles.FirstOrDefault();
        
        if (currentRole != null && currentRole != model.Role)
        {
            await _userManager.RemoveFromRoleAsync(user, currentRole);
            
            if (!await _roleManager.RoleExistsAsync(model.Role))
            {
                await _roleManager.CreateAsync(new IdentityRole(model.Role));
            }
            
            await _userManager.AddToRoleAsync(user, model.Role);
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: Users/Create
    public IActionResult Create()
    {
        ViewBag.Units = _context.Units.Where(u => u.Active).ToList();
        return View();
    }

    // POST: Users/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string email, string password, string role, int? unitId)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ModelState.AddModelError("", "Email e senha são obrigatórios.");
            ViewBag.Units = _context.Units.Where(u => u.Active).ToList();
            return View();
        }

        if (role != "Admin" && role != "Recepcao")
        {
            ModelState.AddModelError("", "Role deve ser Admin ou Recepcao.");
            ViewBag.Units = _context.Units.Where(u => u.Active).ToList();
            return View();
        }

        if (role == "Recepcao" && !unitId.HasValue)
        {
            ModelState.AddModelError("", "Usuário de Recepcao deve estar vinculado a uma unidade.");
            ViewBag.Units = _context.Units.Where(u => u.Active).ToList();
            return View();
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            UnitId = role == "Admin" ? null : unitId
        };

        var result = await _userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            // Criar role se não existir
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }

            await _userManager.AddToRoleAsync(user, role);
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }

        ViewBag.Units = _context.Units.Where(u => u.Active).ToList();
        return View();
    }

    // GET: Users/Delete/5
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Sem role";

        var model = new DeleteUserViewModel
        {
            Id = user.Id,
            Email = user.Email,
            Role = role,
            Unit = user.Unit?.Name ?? "Todas"
        };

        return View(model);
    }

    // POST: Users/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Usuário não encontrado.";
            return RedirectToAction(nameof(Index));
        }

        // Verificar se o usuário logado está tentando se excluir
        var currentUserId = _userManager.GetUserId(User);
        if (user.Id == currentUserId)
        {
            TempData["ErrorMessage"] = "Você não pode excluir seu próprio usuário.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Usuário excluído com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }

        return View(user);
    }
}
