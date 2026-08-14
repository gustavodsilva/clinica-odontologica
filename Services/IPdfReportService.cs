namespace ClinicaOdontologica.Services;

public interface IPdfReportService
{
    byte[] GenerateDailyReport(DateTime date, int? unitId = null);
}
