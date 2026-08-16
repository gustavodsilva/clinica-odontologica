namespace ClinicaOdontologica.Services;

public interface IPdfReportService
{
    byte[] GenerateDailyReport(DateTime startDate, DateTime endDate, int? unitId = null);
}
