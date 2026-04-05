using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosApp.Models;
using PosApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace PosApp.ViewModels;

public partial class AdminViewModel : ViewModelBase
{
    private readonly IDatabaseService _db;
    private readonly IAuthenticationService _auth;
    private readonly INavigationService _nav;

    // --- Simple Business Metrics (Easy English) ---
    [ObservableProperty] private decimal _cashInToday;
    [ObservableProperty] private decimal _profitToday;
    [ObservableProperty] private decimal _totalCashIn;
    [ObservableProperty] private decimal _totalSpending;
    [ObservableProperty] private decimal _netProfit;
    [ObservableProperty] private decimal _stockValue;
    [ObservableProperty] private int _lowStockAlerts;

    // Revenue Breakdowns
    [ObservableProperty] private decimal _cashInLast7Days;
    [ObservableProperty] private decimal _cashInLast30Days;
    [ObservableProperty] private int _totalProducts;
    [ObservableProperty] private int _totalSales;

    // --- Collections ---
    public ObservableCollection<Store> StoresList { get; } = new();
    public ObservableCollection<Product> ProductsList { get; } = new();
    public ObservableCollection<User> UsersList { get; } = new();
    public ObservableCollection<Sale> SalesList { get; } = new();
    public ObservableCollection<TopProductModel> TopProductsList { get; } = new();
    public ObservableCollection<Customer> CustomersList { get; } = new();
    public ObservableCollection<Expense> ExpensesList { get; } = new();
    public ObservableCollection<ArrearsPayment> PaymentHistoryList { get; } = new();
    public ObservableCollection<LedgerEntry> CustomerLedger { get; } = new();

    // --- Selections ---
    [ObservableProperty] private Store? _selectedStore;
    [ObservableProperty] private Product? _selectedProduct;
    [ObservableProperty] private User? _selectedUser;
    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private Expense? _selectedExpense;

    partial void OnSelectedCustomerChanged(Customer? value) => _ = LoadLedgerAsync();

    // --- Dialog/State Properties ---
    [ObservableProperty] private bool _isAddProductDialogOpen;
    [ObservableProperty] private bool _isEditProductDialogOpen;
    [ObservableProperty] private Product _newProduct = new Product { Name = "", Category = "General" };
    [ObservableProperty] private Product? _editingProduct;

    [ObservableProperty] private bool _isAddUserDialogOpen;
    [ObservableProperty] private User _newUser = new User { Username = "", PasswordHash = "", Role = "Cashier" };
    [ObservableProperty] private string _newUserPassword = string.Empty;

    [ObservableProperty] private bool _isAddExpenseDialogOpen;
    [ObservableProperty] private Expense _newExpense = new Expense { Description = "", Category = "Supplies" };

    [ObservableProperty] private bool _isReceivePaymentDialogOpen;
    [ObservableProperty] private decimal _paymentAmount;
    [ObservableProperty] private string _paymentNotes = string.Empty;

    public AdminViewModel(IDatabaseService db, IAuthenticationService auth, INavigationService nav)
    {
        _db = db;
        _auth = auth;
        _nav = nav;
        _ = LoadDataAsync();
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        var stores = await _db.GetStoresAsync();
        var products = await _db.GetProductsAsync();
        var users = await _db.GetUsersAsync();
        var sales = await _db.GetSalesAsync();
        var customers = await _db.GetCustomersAsync();
        var expenses = await _db.GetExpensesAsync();
        var payments = await _db.GetArrearsPaymentHistoryAsync();

        StoresList.Clear(); foreach (var s in stores) StoresList.Add(s);
        ProductsList.Clear(); foreach (var p in products) ProductsList.Add(p);
        UsersList.Clear(); foreach (var u in users) UsersList.Add(u);
        SalesList.Clear(); foreach (var s in sales) SalesList.Add(s);
        CustomersList.Clear(); foreach (var c in customers) CustomersList.Add(c);
        ExpensesList.Clear(); foreach (var e in expenses) ExpensesList.Add(e);
        PaymentHistoryList.Clear(); foreach (var pm in payments) PaymentHistoryList.Add(pm);

        TotalProducts = products.Count;
        TotalSales = sales.Count;
        
        // Stock Value
        StockValue = products.Sum(p => p.Stock * p.CostPrice);
        LowStockAlerts = products.Count(p => p.Stock < 20);

        var today = DateTime.Today;
        var startOfWeek = today.AddDays(-7);
        var startOfMonth = today.AddDays(-30);

        CashInToday = sales.Where(s => s.SaleDate.Date == today).Sum(s => s.GrandTotal);
        ProfitToday = sales.Where(s => s.SaleDate.Date == today)
                           .SelectMany(s => s.SaleItems)
                           .Sum(si => si.Profit);

        CashInLast7Days = sales.Where(s => s.SaleDate.Date >= startOfWeek).Sum(s => s.GrandTotal);
        CashInLast30Days = sales.Where(s => s.SaleDate.Date >= startOfMonth).Sum(s => s.GrandTotal);
        
        TotalCashIn = sales.Sum(s => s.GrandTotal);
        TotalSpending = expenses.Sum(e => e.Amount);
        
        // Net Profit = (All Time Sales Profit) - (All Time Expenses)
        var totalSalesProfit = sales.SelectMany(s => s.SaleItems).Sum(si => si.Profit);
        NetProfit = totalSalesProfit - TotalSpending;

        // Top Products
        var topProducts = sales
            .SelectMany(s => s.SaleItems)
            .GroupBy(si => new { si.ProductId, si.ProductName })
            .Select(g => new TopProductModel
            {
                ProductName = g.Key.ProductName,
                TotalQuantitySold = g.Sum(si => si.Quantity),
                TotalRevenue = g.Sum(si => si.Subtotal)
            })
            .OrderByDescending(tp => tp.TotalQuantitySold)
            .Take(5)
            .ToList();

        TopProductsList.Clear();
        foreach (var tp in topProducts) TopProductsList.Add(tp);
    }

    [RelayCommand]
    private async Task LoadLedgerAsync()
    {
        if (SelectedCustomer == null)
        {
            CustomerLedger.Clear();
            return;
        }

        var sales = await _db.GetSalesAsync();
        var customerSales = sales.Where(s => s.CustomerId == SelectedCustomer.CustomerId).ToList();
        var payments = await _db.GetArrearsPaymentHistoryAsync();
        var customerPayments = payments.Where(p => p.CustomerId == SelectedCustomer.CustomerId).ToList();

        var ledger = new List<LedgerEntry>();
        
        foreach (var s in customerSales)
        {
            ledger.Add(new LedgerEntry 
            { 
                Date = s.SaleDate, 
                Description = $"Sale (Inv #{s.InvoiceNumber})", 
                Debit = s.GrandTotal, 
                Credit = s.CashPaid,
                Reference = s.InvoiceNumber
            });
        }

        foreach (var p in customerPayments)
        {
            ledger.Add(new LedgerEntry 
            { 
                Date = p.Date, 
                Description = string.IsNullOrWhiteSpace(p.Notes) ? "Cash Payment" : p.Notes, 
                Debit = 0, 
                Credit = p.Amount,
                Reference = "PAY-" + p.ArrearsPaymentId
            });
        }

        CustomerLedger.Clear();
        foreach (var entry in ledger.OrderBy(e => e.Date))
        {
            CustomerLedger.Add(entry);
        }
    }

    [RelayCommand]
    private void Logout()
    {
        _auth.Logout();
        _nav.NavigateTo<LoginViewModel>();
    }

    // --- Product Management ---
    [RelayCommand]
    private void OpenAddProductDialog()
    {
        NewProduct = new Product { Name = "", UrduName = "", Price = 0, WholesalePrice = 0, CostPrice = 0, Stock = 0, Barcode = "", Category = "General" };
        IsAddProductDialogOpen = true;
    }

    [RelayCommand]
    private async Task SaveNewProduct()
    {
        if (string.IsNullOrWhiteSpace(NewProduct.Name) || StoresList.Count == 0) return;
        NewProduct.StoreId = StoresList.First().StoreId;
        await _db.AddProductAsync(NewProduct);
        IsAddProductDialogOpen = false;
        await LoadDataAsync();
    }

    [RelayCommand]
    private void OpenEditProductDialog()
    {
        if (SelectedProduct == null) return;
        EditingProduct = new Product 
        { 
            ProductId = SelectedProduct.ProductId,
            Name = SelectedProduct.Name,
            UrduName = SelectedProduct.UrduName,
            Price = SelectedProduct.Price,
            WholesalePrice = SelectedProduct.WholesalePrice,
            CostPrice = SelectedProduct.CostPrice,
            Stock = SelectedProduct.Stock,
            Barcode = SelectedProduct.Barcode,
            Category = SelectedProduct.Category,
            StoreId = SelectedProduct.StoreId
        };
        IsEditProductDialogOpen = true;
    }

    [RelayCommand]
    private async Task UpdateProduct()
    {
        if (EditingProduct == null || string.IsNullOrWhiteSpace(EditingProduct.Name)) return;
        await _db.UpdateProductAsync(EditingProduct);
        IsEditProductDialogOpen = false;
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task DeleteProduct()
    {
        if (SelectedProduct != null)
        {
            await _db.DeleteProductAsync(SelectedProduct.ProductId);
            await LoadDataAsync();
        }
    }

    // --- User Management ---
    [RelayCommand]
    private void OpenAddUserDialog()
    {
        NewUser = new User { Username = "", PasswordHash = "", Role = "Cashier" };
        NewUserPassword = string.Empty;
        IsAddUserDialogOpen = true;
    }

    [RelayCommand]
    private async Task SaveNewUser()
    {
        if (string.IsNullOrWhiteSpace(NewUser.Username) || string.IsNullOrWhiteSpace(NewUserPassword) || StoresList.Count == 0) return;
        NewUser.StoreId = StoresList.First().StoreId;
        NewUser.PasswordHash = _auth.HashPassword(NewUserPassword);
        await _db.AddUserAsync(NewUser);
        IsAddUserDialogOpen = false;
        await LoadDataAsync();
    }

    // --- Expense Management ---
    [RelayCommand]
    private void OpenAddExpenseDialog()
    {
        NewExpense = new Expense { Description = "", Category = "Supplies", Date = DateTime.Now, Amount = 0 };
        IsAddExpenseDialogOpen = true;
    }

    [RelayCommand]
    private async Task SaveNewExpense()
    {
        if (string.IsNullOrWhiteSpace(NewExpense.Description) || NewExpense.Amount <= 0) return;
        await _db.AddExpenseAsync(NewExpense);
        IsAddExpenseDialogOpen = false;
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task DeleteExpense()
    {
        if (SelectedExpense != null)
        {
            await _db.DeleteExpenseAsync(SelectedExpense.ExpenseId);
            await LoadDataAsync();
        }
    }

    // --- Khata (Customer) & Payments ---
    [RelayCommand]
    private void OpenReceivePaymentDialog()
    {
        if (SelectedCustomer == null) return;
        PaymentAmount = SelectedCustomer.ArrearsBalance;
        PaymentNotes = "Paid in cash";
        IsReceivePaymentDialogOpen = true;
    }

    [RelayCommand]
    private async Task ConfirmReceivePayment()
    {
        if (SelectedCustomer == null || PaymentAmount <= 0) return;
        await _db.ReceivePaymentAsync(SelectedCustomer.CustomerId, PaymentAmount, PaymentNotes);
        IsReceivePaymentDialogOpen = false;
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task DeleteCustomer()
    {
        if (SelectedCustomer != null)
        {
            await _db.DeleteCustomerAsync(SelectedCustomer.CustomerId);
            await LoadDataAsync();
        }
    }
}

public class LedgerEntry
{
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }  // Udhaar increase
    public decimal Credit { get; set; } // Cash received
    public string Reference { get; set; } = string.Empty;
}

public class TopProductModel
{
    public string ProductName { get; set; } = string.Empty;
    public int TotalQuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
}

