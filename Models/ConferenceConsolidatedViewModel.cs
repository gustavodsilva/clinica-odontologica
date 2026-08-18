namespace ClinicaOdontologica.Models;

public class ConferenceConsolidatedViewModel
{
    public int PaymentMethodId { get; set; }
    public string PaymentMethodName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal NetAmount { get; set; }
    public int PendingCount { get; set; }
    public int ConfirmedCount { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal ConfirmedAmount { get; set; }
}
