using Microsoft.EntityFrameworkCore;
using PosApp.Data;
using PosApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PosApp.Services;

public interface IDatabaseService
{
    Task EnsureCreatedAsync();
    
    // Stores
    Task<List<Store>> GetStoresAsync();
    Task AddStoreAsync(Store store);
    Task UpdateStoreAsync(Store store);
    Task DeleteStoreAsync(int storeId);

    // Products
    Task<List<Product>> GetProductsAsync(int? storeId = null);
    Task AddProductAsync(Product product);
    Task UpdateProductAsync(Product product);
    Task DeleteProductAsync(int productId);

    // Users
    Task<List<User>> GetUsersAsync();
    Task AddUserAsync(User user);
    Task UpdateUserAsync(User user);
    Task DeleteUserAsync(int userId);

    // Sales
    Task<List<Sale>> GetSalesAsync(int? storeId = null, System.DateTime? date = null);
    Task AddSaleAsync(Sale sale);

    // Customers (Khata)
    Task<List<Customer>> GetCustomersAsync();
    Task AddCustomerAsync(Customer customer);
    Task UpdateCustomerAsync(Customer customer);
    Task DeleteCustomerAsync(int customerId);
    Task ReceivePaymentAsync(int customerId, decimal amount, string? notes = null);
    Task<List<ArrearsPayment>> GetArrearsPaymentHistoryAsync(int? customerId = null);

    // Expenses
    Task<List<Expense>> GetExpensesAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task AddExpenseAsync(Expense expense);
    Task DeleteExpenseAsync(int expenseId);
}

public class DatabaseService : IDatabaseService
{
    public async Task EnsureCreatedAsync()
    {
        using var context = new AppDbContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task<List<Store>> GetStoresAsync()
    {
        using var context = new AppDbContext();
        return await context.Stores.ToListAsync();
    }

    public async Task AddStoreAsync(Store store)
    {
        using var context = new AppDbContext();
        context.Stores.Add(store);
        await context.SaveChangesAsync();
    }

    public async Task UpdateStoreAsync(Store store)
    {
        using var context = new AppDbContext();
        context.Stores.Update(store);
        await context.SaveChangesAsync();
    }

    public async Task DeleteStoreAsync(int storeId)
    {
        using var context = new AppDbContext();
        var store = await context.Stores.FindAsync(storeId);
        if (store != null)
        {
            context.Stores.Remove(store);
            await context.SaveChangesAsync();
        }
    }

    public async Task<List<Product>> GetProductsAsync(int? storeId = null)
    {
        using var context = new AppDbContext();
        var query = context.Products.Include(p => p.Store).AsQueryable();
        if (storeId.HasValue)
            query = query.Where(p => p.StoreId == storeId.Value);
        return await query.ToListAsync();
    }

    public async Task AddProductAsync(Product product)
    {
        using var context = new AppDbContext();
        context.Products.Add(product);
        await context.SaveChangesAsync();
    }

    public async Task UpdateProductAsync(Product product)
    {
        using var context = new AppDbContext();
        context.Products.Update(product);
        await context.SaveChangesAsync();
    }

    public async Task DeleteProductAsync(int productId)
    {
        using var context = new AppDbContext();
        var product = await context.Products.FindAsync(productId);
        if (product != null)
        {
            context.Products.Remove(product);
            await context.SaveChangesAsync();
        }
    }

    public async Task<List<User>> GetUsersAsync()
    {
        using var context = new AppDbContext();
        return await context.Users.Include(u => u.Store).ToListAsync();
    }

    public async Task AddUserAsync(User user)
    {
        using var context = new AppDbContext();
        context.Users.Add(user);
        await context.SaveChangesAsync();
    }

    public async Task UpdateUserAsync(User user)
    {
        using var context = new AppDbContext();
        context.Users.Update(user);
        await context.SaveChangesAsync();
    }

    public async Task DeleteUserAsync(int userId)
    {
        using var context = new AppDbContext();
        var user = await context.Users.FindAsync(userId);
        if (user != null)
        {
            context.Users.Remove(user);
            await context.SaveChangesAsync();
        }
    }

    public async Task<List<Sale>> GetSalesAsync(int? storeId = null, System.DateTime? date = null)
    {
        using var context = new AppDbContext();
        var query = context.Sales
            .Include(s => s.Store)
            .Include(s => s.User)
            .Include(s => s.Customer)
            .Include(s => s.SaleItems)
            .ThenInclude(si => si.Product)
            .AsQueryable();

        if (storeId.HasValue)
            query = query.Where(s => s.StoreId == storeId.Value);
        if (date.HasValue)
            query = query.Where(s => s.SaleDate.Date == date.Value.Date);

        return await query.OrderByDescending(s => s.SaleDate).ToListAsync();
    }

    public async Task AddSaleAsync(Sale sale)
    {
        using var context = new AppDbContext();

        // Calculate arrears
        sale.ArrearsAdded = sale.GrandTotal - sale.CashPaid;
        if (sale.ArrearsAdded < 0) sale.ArrearsAdded = 0;

        context.Sales.Add(sale);

        // Update product stock (skip custom items with null ProductId)
        foreach (var item in sale.SaleItems)
        {
            if (item.ProductId.HasValue && item.ProductId.Value > 0)
            {
                var product = await context.Products.FindAsync(item.ProductId.Value);
                if (product != null)
                {
                    product.Stock -= item.Quantity;
                    context.Products.Update(product);
                    
                    // Lock in cost and profit
                    item.UnitCost = product.CostPrice;
                    item.Profit = (item.UnitPrice - item.UnitCost) * item.Quantity;
                }
            }
            else
            {
                // For custom items, assume 50% profit margin as a safe approximation
                item.UnitCost = item.UnitPrice * 0.5m;
                item.Profit = item.Subtotal - (item.UnitCost * item.Quantity);
            }
        }

        // Update customer arrears balance
        if (sale.CustomerId.HasValue && sale.ArrearsAdded > 0)
        {
            var customer = await context.Customers.FindAsync(sale.CustomerId.Value);
            if (customer != null)
            {
                customer.ArrearsBalance += sale.ArrearsAdded;
                context.Customers.Update(customer);
            }
        }

        await context.SaveChangesAsync();
    }

    // --- Customer (Khata) ---

    public async Task<List<Customer>> GetCustomersAsync()
    {
        using var context = new AppDbContext();
        return await context.Customers.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task AddCustomerAsync(Customer customer)
    {
        using var context = new AppDbContext();
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
    }

    public async Task UpdateCustomerAsync(Customer customer)
    {
        using var context = new AppDbContext();
        context.Customers.Update(customer);
        await context.SaveChangesAsync();
    }

    public async Task DeleteCustomerAsync(int customerId)
    {
        using var context = new AppDbContext();
        var customer = await context.Customers.FindAsync(customerId);
        if (customer != null)
        {
            context.Customers.Remove(customer);
            await context.SaveChangesAsync();
        }
    }

    public async Task ReceivePaymentAsync(int customerId, decimal amount, string? notes = null)
    {
        using var context = new AppDbContext();
        var customer = await context.Customers.FindAsync(customerId);
        if (customer != null)
        {
            customer.ArrearsBalance -= amount;
            if (customer.ArrearsBalance < 0) customer.ArrearsBalance = 0;
            context.Customers.Update(customer);

            // Log the payment history
            context.ArrearsPayments.Add(new ArrearsPayment
            {
                CustomerId = customerId,
                Amount = amount,
                Date = DateTime.Now,
                Notes = notes ?? "Cash Payment"
            });

            await context.SaveChangesAsync();
        }
    }

    public async Task<List<ArrearsPayment>> GetArrearsPaymentHistoryAsync(int? customerId = null)
    {
        using var context = new AppDbContext();
        var query = context.ArrearsPayments.Include(p => p.Customer).AsQueryable();
        if (customerId.HasValue)
            query = query.Where(p => p.CustomerId == customerId.Value);
        return await query.OrderByDescending(p => p.Date).ToListAsync();
    }

    // --- Expenses ---

    public async Task<List<Expense>> GetExpensesAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        using var context = new AppDbContext();
        var query = context.Expenses.AsQueryable();
        if (fromDate.HasValue)
            query = query.Where(e => e.Date >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(e => e.Date <= toDate.Value);
        return await query.OrderByDescending(e => e.Date).ToListAsync();
    }

    public async Task AddExpenseAsync(Expense expense)
    {
        using var context = new AppDbContext();
        context.Expenses.Add(expense);
        await context.SaveChangesAsync();
    }

    public async Task DeleteExpenseAsync(int expenseId)
    {
        using var context = new AppDbContext();
        var expense = await context.Expenses.FindAsync(expenseId);
        if (expense != null)
        {
            context.Expenses.Remove(expense);
            await context.SaveChangesAsync();
        }
    }
}

