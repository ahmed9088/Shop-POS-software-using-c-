using System;

namespace PosApp.Models;

public class Expense
{
    public int ExpenseId { get; set; }
    public required string Description { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public required string Category { get; set; } // Rent, Electricity, Supplies, etc.
}
