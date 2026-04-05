namespace PosApp.Models;

public class SaleItem
{
    public int SaleItemId { get; set; }
    
    public int SaleId { get; set; }
    public Sale? Sale { get; set; }
    
    public int? ProductId { get; set; }
    public Product? Product { get; set; }
    
    public string ProductName { get; set; } = string.Empty; // Snapshot name at time of sale
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; } // Snapshot price at time of sale
    public decimal UnitCost { get; set; } // Snapshot cost at time of sale
    public decimal Subtotal { get; set; }
    public decimal Profit { get; set; }
}
