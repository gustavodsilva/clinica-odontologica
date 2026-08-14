namespace ClinicaOdontologica.Models;

public class PaymentLog
{
    public int Id { get; set; }
    public int PaymentId { get; set; }
    public string Action { get; set; } = string.Empty; // Created, Edited, StatusChanged
    public string ChangedBy { get; set; } = string.Empty; // Email do usuário
    public string? OldValue { get; set; } // JSON com valores antigos (opcional)
    public string? NewValue { get; set; } // JSON com valores novos (opcional)
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Payment? Payment { get; set; }
}
