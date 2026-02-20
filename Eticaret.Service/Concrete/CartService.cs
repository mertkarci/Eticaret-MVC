using Eticaret.Core;
using Eticaret.Core.Entities;

namespace Eticaret.Service.Concrete;

public class CartService : ICartService
{
    public List<CartLine> CartLines = new();
    public void AddProduct(Product product, int quantity)
    {
        var _product = CartLines.FirstOrDefault(p => p.Product.Id == product.Id);

        if (_product != null)
        {
            _product.Quantity += quantity;
        }
        else
        {
            CartLines.Add(new CartLine()
            {
                Product = product,
                Quantity = quantity
            });
        }

    }

    public void ClearAll()
    {
        CartLines.Clear();
    }

    public void RemoveProduct(Product product)
    {
        CartLines.RemoveAll(p => p.Product.Id == product.Id);
    }

    public decimal TotalPrice()
    {
        return CartLines.Sum(p => p.Product.Price * p.Quantity);
    }

    public void UpdateProduct(Product product, int quantity)
    {
        var _product = CartLines.FirstOrDefault(p => p.Product.Id == product.Id);

        if (_product != null)
        {
            _product.Quantity = quantity;
        }
        else
        {
            CartLines.Add(new CartLine()
            {
                Product = product,
                Quantity = quantity
            });
        }
    }
}
