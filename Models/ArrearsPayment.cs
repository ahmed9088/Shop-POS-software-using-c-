using System;

namespace PosApp.Models;

public class ArrearsPayment
{
    public int ArrearsPaymentId { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; } // Cash, Bank, etc.
}
