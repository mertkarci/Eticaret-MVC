using Eticaret.Core.Entities;

namespace Eticaret.Core;

public class CartLine
{
    public int Id {get; set;}
    public Product Product {get; set;}
    public int Quantity {get; set;}
}
