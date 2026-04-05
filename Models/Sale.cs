using System;
using System.Collections.Generic;

namespace PosApp.Models;

public class Sale
{
    public int SaleId { get; set; }
    public required string InvoiceNumber { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal GrandTotal { get; set; }
    
    public int StoreId { get; set; }
    public Store? Store { get; set; }
    
    public int UserId { get; set; }
    public User? User { get; set; }
    
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public decimal CashPaid { get; set; }
    public decimal ArrearsAdded { get; set; }

    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
