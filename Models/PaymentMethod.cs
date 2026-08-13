namespace ClinicaOdontologica.Models;

public class PaymentMethod
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool RequiresBrand { get; set; }
    public bool RequiresInstallments { get; set; }
    public bool Active { get; set; } = true;
}
