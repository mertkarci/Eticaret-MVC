using Eticaret.Core.Entities;
using Eticaret.Service.Abstract;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eticaret.Service.Concrete
{
    public class BrandService : IBrandService
    {
        private readonly IService<Brand> _brandService;
        private readonly IMemoryCache _memoryCache;
        private const string AllBrandsCacheKey = "AllBrands";

        public BrandService(IService<Brand> brandService, IMemoryCache memoryCache)
        {
            _brandService = brandService;
            _memoryCache = memoryCache;
        }

        public async Task<List<Brand>> GetAllBrandsAsync()
        {
            if (!_memoryCache.TryGetValue(AllBrandsCacheKey, out List<Brand> brands))
            {
                brands = await _brandService.GetAllAsync();
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(2));
                _memoryCache.Set(AllBrandsCacheKey, brands, cacheOptions);
            }
            return brands;
        }

        public void ClearBrandsCache()
        {
            _memoryCache.Remove(AllBrandsCacheKey); 
        }
    }
}
