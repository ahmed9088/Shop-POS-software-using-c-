using System.Collections.Generic;

namespace PosApp.Models;

public class Store
{
    public int StoreId { get; set; }
    public required string Name { get; set; }
    public string? Location { get; set; }
    public string? Address { get; set; }
    public string? ContactNumber { get; set; }
    public string? UrduTagline { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}

