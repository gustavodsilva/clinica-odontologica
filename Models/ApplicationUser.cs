using Microsoft.AspNetCore.Identity;

namespace ClinicaOdontologica.Models;

public class ApplicationUser : IdentityUser
{
    public int? UnitId { get; set; }
    public Unit? Unit { get; set; }
}
