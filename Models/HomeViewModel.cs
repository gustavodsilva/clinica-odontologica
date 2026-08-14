namespace ClinicaOdontologica.Models;

public class HomeViewModel
{
    public DateTime TodayDate { get; set; }
    public int TotalPayments { get; set; }
    public int PendingPayments { get; set; }
    public int ConfirmedPayments { get; set; }
    public decimal TotalGrossAmount { get; set; }
    public List<PaymentByUnitSummary> PaymentsByUnit { get; set; } = new();
}

public class PaymentByUnitSummary
{
    public string UnitName { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Pending { get; set; }
    public int Confirmed { get; set; }
    public decimal GrossAmount { get; set; }
}
