using Eticaret.Core.Entities;

namespace Eticaret.WebUI.Areas.Admin.Models
{
    public class UserDetailsViewModel
    {
        public AppUser User { get; set; }
        public IEnumerable<Order> Orders { get; set; }
        public IEnumerable<Address> Addresses { get; set; }
    }
}