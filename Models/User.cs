using System;

namespace PosApp.Models;

public class User
{
    public int UserId { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public required string Role { get; set; } // "Admin" or "Cashier"
    
    public int? StoreId { get; set; } // Which store the cashier belongs to
    public Store? Store { get; set; }
}
