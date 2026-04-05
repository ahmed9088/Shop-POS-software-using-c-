namespace PosApp.Models;

public class Product
{
    public int ProductId { get; set; }
    public required string Name { get; set; }
    public string? UrduName { get; set; }
    public decimal Price { get; set; }
    public decimal WholesalePrice { get; set; }
    public decimal CostPrice { get; set; }
    public int Stock { get; set; }
    public string? Barcode { get; set; }
    public string Category { get; set; } = "General"; // Photocopy, Printing, Binding, etc.
    public bool IsActive { get; set; } = true;
    
    public int StoreId { get; set; }
    public Store? Store { get; set; }
}

