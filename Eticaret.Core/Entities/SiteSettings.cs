using Eticaret.Core.Entities;

namespace Eticaret.Core.Entities;

public class SiteSettings : IEntity
{
    public int Id {get; set;}
    public bool IsMaintenanceMode { get; set; }
    public string MaintenanceMessage { get; set; }
}
