

using Eticaret.Service.Abstract;

namespace Eticaret.Service.Concrete;

public class MaintenanceService : IMaintenanceService
{
    private bool _isMaintenance;

    public bool IsEnabled() => _isMaintenance;

    public void SetMaintenanceMode(bool state)
    {
        _isMaintenance = state;

    }
}