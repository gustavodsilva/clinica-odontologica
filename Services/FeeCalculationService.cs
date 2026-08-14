using ClinicaOdontologica.Data;
using ClinicaOdontologica.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicaOdontologica.Services;

public class FeeCalculationService : IFeeCalculationService
{
    private readonly ApplicationDbContext _context;

    public FeeCalculationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public (decimal feePercentage, decimal feeAmount, decimal netAmount) Calculate(
        int paymentMethodId,
        int? cardBrandId,
        int? installments,
        decimal grossAmount)
    {
        // Buscar regra ativa que combina exatamente payment_method_id + card_brand_id + installments
        var feeRule = _context.FeeRules
            .FirstOrDefault(f => f.PaymentMethodId == paymentMethodId
                && f.CardBrandId == cardBrandId
                && f.Installments == installments
                && f.Active);

        if (feeRule == null)
        {
            throw new InvalidOperationException(
                "Não foi encontrada uma regra de taxa ativa para esta combinação de forma de pagamento, bandeira e parcelas. " +
                "Entre em contato com o Admin para cadastrar a taxa antes de lançar o pagamento.");
        }

        // Calcular: fee_amount = gross_amount * (fee_percentage / 100)
        var feeAmount = grossAmount * (feeRule.FeePercentage / 100);
        var netAmount = grossAmount - feeAmount;

        return (feeRule.FeePercentage, feeAmount, netAmount);
    }
}
