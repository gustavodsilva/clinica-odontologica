using ClinicaOdontologica.Data;
using ClinicaOdontologica.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ClinicaOdontologica.Services;

public class PdfReportService : IPdfReportService
{
    private readonly ApplicationDbContext _context;

    public PdfReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public byte[] GenerateDailyReport(DateTime date, int? unitId = null)
    {
        IQueryable<Payment> query = _context.Payments
            .Include(p => p.Unit)
            .Include(p => p.PaymentMethod)
            .Include(p => p.CardBrand)
            .Where(p => p.PaymentDate.Date == date);

        if (unitId.HasValue)
        {
            query = query.Where(p => p.UnitId == unitId.Value);
        }

        var payments = query.OrderBy(p => p.Unit!.Name).ThenBy(p => p.CreatedAt).ToList();

        var sb = new StringBuilder();

        // Cabeçalho do cupom fiscal
        sb.AppendLine("========================================");
        sb.AppendLine("       CLINICA RIS.O.S");
        sb.AppendLine("   RELATORIO DE CONCILIACAO DIARIA");
        sb.AppendLine($"   DATA: {date:dd/MM/yyyy}");
        sb.AppendLine("========================================");
        sb.AppendLine();

        // Resumo geral
        sb.AppendLine("RESUMO GERAL:");
        sb.AppendLine($"Total de Pagamentos: {payments.Count}");
        sb.AppendLine($"Pendentes: {payments.Count(p => p.Status == PaymentStatus.Pendente)}");
        sb.AppendLine($"Conferidos (OK): {payments.Count(p => p.Status == PaymentStatus.OK)}");
        sb.AppendLine($"Valor Bruto Total: {payments.Sum(p => p.GrossAmount):C2}");
        sb.AppendLine();
        sb.AppendLine("========================================");
        sb.AppendLine();

        // Detalhes dos pagamentos
        sb.AppendLine("DETALHES DOS PAGAMENTOS:");
        sb.AppendLine("----------------------------------------");
        
        foreach (var payment in payments)
        {
            sb.AppendLine($"Paciente: {payment.PatientCode}");
            sb.AppendLine($"Unidade: {payment.Unit?.Name ?? "-"}");
            sb.AppendLine($"Data: {payment.PaymentDate:dd/MM/yyyy}");
            sb.AppendLine($"Valor: {payment.GrossAmount:C2}");
            
            var paymentMethod = payment.PaymentMethod?.Name ?? "-";
            var cardBrand = payment.CardBrand != null ? $" + {payment.CardBrand.Name}" : "";
            var installments = payment.Installments.HasValue ? $" ({payment.Installments}x)" : "";
            sb.AppendLine($"Forma: {paymentMethod}{cardBrand}{installments}");
            
            sb.AppendLine($"Taxa: {payment.FeePercentageApplied:F2}% ({payment.FeeAmount:C2})");
            sb.AppendLine($"Valor Liquido: {payment.NetAmountExpected:C2}");
            sb.AppendLine($"Status: {payment.Status}");
            sb.AppendLine("----------------------------------------");
        }

        sb.AppendLine();
        sb.AppendLine("========================================");
        sb.AppendLine($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        sb.AppendLine("========================================");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
