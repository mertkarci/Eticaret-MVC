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
    
            if (_product.Quantity + quantity <= product.Stock)
            {
                _product.Quantity += quantity;
            }
        }
        else
        {
            if (quantity <= product.Stock)
            {
                CartLines.Add(new CartLine() { Product = product, Quantity = quantity });
            }
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

    public int TotalQuantity()
    {
        return CartLines.Sum(p => p.Quantity);
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
            int newQuantity = _product.Quantity + quantity;
            
            if (newQuantity <= 0)
            {
                CartLines.RemoveAll(p => p.Product.Id == product.Id);
            }
            else if (newQuantity <= product.Stock) 
            {
                _product.Quantity = newQuantity;
            }
        }
        else
        {
            if (quantity <= product.Stock && quantity > 0)
            {
                CartLines.Add(new CartLine() { Product = product, Quantity = quantity });
            }
        }
    }

    public decimal ShippingCost()
    {
        return TotalPrice() >= 1000m || TotalPrice() == 0 ? 0m : 99m;
    }

    public decimal GrandTotal()
    {
        return TotalPrice() + ShippingCost();
    }
}
