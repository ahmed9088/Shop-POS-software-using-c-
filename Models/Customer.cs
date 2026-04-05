using System.Collections.Generic;

namespace PosApp.Models;

public class Customer
{
    public int CustomerId { get; set; }
    public required string Name { get; set; }
    public string? Phone { get; set; }
    public decimal ArrearsBalance { get; set; }

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public ICollection<ArrearsPayment> ArrearsPayments { get; set; } = new List<ArrearsPayment>();
}
