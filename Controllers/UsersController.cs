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
}
