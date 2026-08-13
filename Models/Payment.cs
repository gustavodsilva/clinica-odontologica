namespace ClinicaOdontologica.Models;

public enum PaymentStatus
{
    Pendente,
    OK
}

public class Payment
{
    public int Id { get; set; }
    public int UnitId { get; set; }
    public string PatientCode { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public decimal GrossAmount { get; set; }
    public int PaymentMethodId { get; set; }
    public int? CardBrandId { get; set; }
    public int? Installments { get; set; }
    public decimal FeePercentageApplied { get; set; }
    public decimal FeeAmount { get; set; }
    public decimal NetAmountExpected { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pendente;
    public string? ConfirmedBy { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Unit? Unit { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public CardBrand? CardBrand { get; set; }
}
