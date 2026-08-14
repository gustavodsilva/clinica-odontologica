namespace ClinicaOdontologica.Services;

public interface IBusinessDayService
{
    DateTime GetNextBusinessDay(DateTime date);
}
