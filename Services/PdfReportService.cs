using ClinicaOdontologica.Data;
using ClinicaOdontologica.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace ClinicaOdontologica.Services;

public class PdfReportService : IPdfReportService
{
    private readonly ApplicationDbContext _context;

    public PdfReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    // Helper method para remover acentos
    private string RemoveAccents(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    public byte[] GenerateDailyReport(DateTime startDate, DateTime endDate, int? unitId = null)
    {
        IQueryable<Payment> query = _context.Payments
            .Include(p => p.Unit)
            .Include(p => p.PaymentMethod)
            .Include(p => p.CardBrand)
            .Where(p => p.PaymentDate.Date >= startDate.Date && p.PaymentDate.Date <= endDate.Date);

        if (unitId.HasValue)
        {
            query = query.Where(p => p.UnitId == unitId.Value);
        }

        var payments = query.OrderBy(p => p.Unit!.Name).ThenBy(p => p.CreatedAt).ToList();

        var sb = new StringBuilder();

        // Cabeçalho do cupom fiscal
        sb.AppendLine("========================================");
        sb.AppendLine("       CLINICA RIS.O.S");
        sb.AppendLine("   RELATORIO DE CONCILIACAO");
        sb.AppendLine($"   PERIODO: {startDate:dd/MM/yyyy} a {endDate:dd/MM/yyyy}");
        sb.AppendLine("========================================");
        sb.AppendLine();

        // Resumo geral
        sb.AppendLine("RESUMO GERAL:");
        sb.AppendLine($"Total de Pagamentos: {payments.Count}");
        sb.AppendLine($"Pendentes: {payments.Count(p => p.Status == PaymentStatus.Pendente)}");
        sb.AppendLine($"Conferidos (OK): {payments.Count(p => p.Status == PaymentStatus.OK)}");
        sb.AppendLine($"Valor Bruto Total: {payments.Sum(p => p.GrossAmount):C2}");
        sb.AppendLine($"Valor Liquido Total: {payments.Sum(p => p.NetAmountExpected):C2}");
        sb.AppendLine();
        sb.AppendLine("========================================");
        sb.AppendLine();

        // Resumo por forma de pagamento
        sb.AppendLine("RESUMO POR FORMA DE PAGAMENTO:");
        sb.AppendLine("------------------------------");

        var paymentsByMethod = payments
            .Where(p => p.PaymentMethod != null)
            .GroupBy(p => new { p.PaymentMethodId, p.PaymentMethod!.Name })
            .OrderBy(g => g.Key.Name)
            .ToList();

        foreach (var group in paymentsByMethod)
        {
            var methodName = group.Key.Name.ToUpper();
            var count = group.Count();
            var grossTotal = group.Sum(p => p.GrossAmount);
            var netTotal = group.Sum(p => p.NetAmountExpected);
            var pendingCount = group.Count(p => p.Status == PaymentStatus.Pendente);
            var confirmedCount = group.Count(p => p.Status == PaymentStatus.OK);

            sb.AppendLine($"{methodName}:");
            sb.AppendLine($"Quantidade: {count}");
            sb.AppendLine($"Valor Bruto Total: {grossTotal:C2}");
            sb.AppendLine($"Valor Liquido Total: {netTotal:C2}");

            // Status da conferência (se não for cartão)
            if (!RemoveAccents(methodName).Contains("CARTAO"))
            {
                if (pendingCount == 0)
                {
                    sb.AppendLine($"Status: OK");
                }
                else if (confirmedCount == 0)
                {
                    sb.AppendLine($"Status: Pendente");
                }
                else
                {
                    sb.AppendLine($"Status: Parcial ({confirmedCount} OK, {pendingCount} Pendente)");
                }
            }

            sb.AppendLine();
        }

        sb.AppendLine("========================================");
        sb.AppendLine();

        // Separar cartões (crédito e débito)
        var cardPayments = payments.Where(p => p.PaymentMethod != null && 
            RemoveAccents(p.PaymentMethod.Name.ToLower()).Contains("cartao")).ToList();

        if (cardPayments.Any())
        {
            sb.AppendLine("CARTOES:");
            sb.AppendLine($"Quantidade: {cardPayments.Count}");
            sb.AppendLine($"Valor Bruto Total: {cardPayments.Sum(p => p.GrossAmount):C2}");
            sb.AppendLine($"Valor Liquido Total: {cardPayments.Sum(p => p.NetAmountExpected):C2}");
            sb.AppendLine();
            
            var creditCardPayments = cardPayments.Where(p => p.PaymentMethod != null && 
                RemoveAccents(p.PaymentMethod.Name.ToLower()).Contains("credito")).ToList();
            var debitCardPayments = cardPayments.Where(p => p.PaymentMethod != null && 
                RemoveAccents(p.PaymentMethod.Name.ToLower()).Contains("debito")).ToList();

            if (creditCardPayments.Any())
            {
                sb.AppendLine("CARTAO DE CREDITO:");
                sb.AppendLine($"Quantidade: {creditCardPayments.Count}");
                sb.AppendLine($"Valor Bruto Total: {creditCardPayments.Sum(p => p.GrossAmount):C2}");
                sb.AppendLine($"Valor Liquido Total: {creditCardPayments.Sum(p => p.NetAmountExpected):C2}");
                
                // Agrupar por data de recebimento esperada
                var byReceiptDate = creditCardPayments
                    .Where(p => p.ExpectedReceiptDate.HasValue)
                    .GroupBy(p => p.ExpectedReceiptDate!.Value.Date);
                
                foreach (var group in byReceiptDate)
                {
                    sb.AppendLine($"A receber em {group.Key:dd/MM/yyyy}: {group.Sum(p => p.NetAmountExpected):C2}");
                }
                sb.AppendLine();
            }

            if (debitCardPayments.Any())
            {
                sb.AppendLine("CARTAO DE DEBITO:");
                sb.AppendLine($"Quantidade: {debitCardPayments.Count}");
                sb.AppendLine($"Valor Bruto Total: {debitCardPayments.Sum(p => p.GrossAmount):C2}");
                sb.AppendLine($"Valor Liquido Total: {debitCardPayments.Sum(p => p.NetAmountExpected):C2}");
                
                // Agrupar por data de recebimento esperada
                var byReceiptDate = debitCardPayments
                    .Where(p => p.ExpectedReceiptDate.HasValue)
                    .GroupBy(p => p.ExpectedReceiptDate!.Value.Date);
                
                foreach (var group in byReceiptDate)
                {
                    sb.AppendLine($"A receber em {group.Key:dd/MM/yyyy}: {group.Sum(p => p.NetAmountExpected):C2}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("========================================");
            sb.AppendLine();
        }

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
            
            if (payment.ExpectedReceiptDate.HasValue)
            {
                sb.AppendLine($"Data Recebimento Esperado: {payment.ExpectedReceiptDate.Value:dd/MM/yyyy}");
            }
            
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
