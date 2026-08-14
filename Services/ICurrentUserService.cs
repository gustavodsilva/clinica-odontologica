namespace ClinicaOdontologica.Services;

public interface ICurrentUserService
{
    int? GetCurrentUnitId();
    string GetCurrentUserId();
    bool IsAdmin();
}
