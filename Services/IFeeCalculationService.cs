namespace ClinicaOdontologica.Services;

public interface IFeeCalculationService
{
    (decimal feePercentage, decimal feeAmount, decimal netAmount) Calculate(
        int paymentMethodId, 
        int? cardBrandId, 
        int? installments, 
        decimal grossAmount);
}
