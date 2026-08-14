namespace ClinicaOdontologica.Services;

public class BusinessDayService : IBusinessDayService
{
    public DateTime GetNextBusinessDay(DateTime date)
    {
        var nextDay = date.AddDays(1);
        
        // Pular fins de semana (sábado = 6, domingo = 0)
        while (nextDay.DayOfWeek == DayOfWeek.Saturday || nextDay.DayOfWeek == DayOfWeek.Sunday)
        {
            nextDay = nextDay.AddDays(1);
        }
        
        return nextDay;
    }
}
