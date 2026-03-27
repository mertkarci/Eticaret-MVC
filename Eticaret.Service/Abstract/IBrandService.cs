using Eticaret.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eticaret.Service.Abstract
{
    public interface IBrandService
    {
        Task<List<Brand>> GetAllBrandsAsync();
        void ClearBrandsCache();
    }
}
