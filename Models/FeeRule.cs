using System.ComponentModel.DataAnnotations;

namespace ClinicaOdontologica.Models;

public class FeeRule
{
    public int Id { get; set; }
    public int PaymentMethodId { get; set; }
    public int? CardBrandId { get; set; }
    public int? Installments { get; set; }

    [Range(0, 100, ErrorMessage = "A taxa deve estar entre 0 e 100.")]
    public decimal FeePercentage { get; set; }

    public bool Active { get; set; } = true;

    // Navigation properties
    public PaymentMethod? PaymentMethod { get; set; }
    public CardBrand? CardBrand { get; set; }
}
