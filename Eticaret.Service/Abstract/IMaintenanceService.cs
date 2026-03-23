
namespace Eticaret.Service.Abstract;
public interface IMaintenanceService
{
    bool IsEnabled();
    void SetMaintenanceMode(bool state);
}

