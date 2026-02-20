using Eticaret.Core.Entities;

namespace Eticaret.Service;

public interface ICartService
{
    void AddProduct(Product product, int quantity);
    void UpdateProduct(Product product, int quantity);
    void RemoveProduct(Product product);
    decimal TotalPrice();
    void ClearAll();
}
