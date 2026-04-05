using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosApp.Models;
using PosApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace PosApp.ViewModels;

public partial class PosViewModel : ViewModelBase
{
    private readonly IDatabaseService _db;
    private readonly IAuthenticationService _auth;
    private readonly INavigationService _nav;

    public ObservableCollection<Product> ProductsList { get; } = new();
    public ObservableCollection<CartItemViewModel> Cart { get; } = new();
    public ObservableCollection<Customer> CustomersList { get; } = new();
    public ObservableCollection<string> Categories { get; } = new();

    private System.Collections.Generic.List<Product> _allProducts = new();

    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private string? _selectedCategory;
    [ObservableProperty] private bool _isWholesaleMode;

    partial void OnIsWholesaleModeChanged(bool value)
    {
        foreach (var item in Cart) item.RefreshPrice();
        UpdateTotals();
    }

    partial void OnSearchQueryChanged(string value) => FilterProducts();
    partial void OnSelectedCategoryChanged(string? value) => FilterProducts();

    private void FilterProducts()
    {
        var filtered = _allProducts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var lowerQ = SearchQuery.Trim().ToLowerInvariant();
            filtered = filtered.Where(p =>
                p.Name.ToLowerInvariant().Contains(lowerQ) ||
                (p.UrduName != null && p.UrduName.Contains(SearchQuery)) ||
                (p.Barcode != null && p.Barcode.ToLowerInvariant().Contains(lowerQ)) ||
                p.Category.ToLowerInvariant().Contains(lowerQ)
            );
        }

        if (!string.IsNullOrWhiteSpace(SelectedCategory) && SelectedCategory != "All")
        {
            filtered = filtered.Where(p => p.Category == SelectedCategory);
        }

        ProductsList.Clear();
        foreach (var p in filtered) ProductsList.Add(p);
    }

    [ObservableProperty] private decimal _subTotal;
    [ObservableProperty] private decimal _tax;
    [ObservableProperty] private decimal _grandTotal;

    public PosViewModel(IDatabaseService db, IAuthenticationService auth, INavigationService nav)
    {
        _db = db;
        _auth = auth;
        _nav = nav;
        _ = LoadProductsAsync();
        _ = LoadCustomersAsync();
    }

    [RelayCommand]
    private async Task LoadProductsAsync()
    {
        if (_auth.CurrentUser == null) return;
        var products = await _db.GetProductsAsync(_auth.CurrentUser.StoreId);
        _allProducts = products.ToList();
        
        Categories.Clear();
        Categories.Add("All");
        foreach (var cat in _allProducts.Select(p => p.Category).Distinct().OrderBy(c => c))
        {
            Categories.Add(cat);
        }
        SelectedCategory = "All";
        
        FilterProducts();
    }

    private async Task LoadCustomersAsync()
    {
        var customers = await _db.GetCustomersAsync();
        CustomersList.Clear();
        foreach (var c in customers) CustomersList.Add(c);
    }

    [RelayCommand]
    private void AddToCart(Product? product)
    {
        if (product == null) return;
        var existingItem = Cart.FirstOrDefault(c => c.Product.ProductId == product.ProductId);
        if (existingItem != null)
            existingItem.Quantity++;
        else
            Cart.Add(new CartItemViewModel(product, this));
        UpdateTotals();
    }

    [RelayCommand]
    private void RemoveFromCart(CartItemViewModel? cartItem)
    {
        if (cartItem == null) return;
        Cart.Remove(cartItem);
        UpdateTotals();
    }

    [RelayCommand]
    private void ClearCart()
    {
        Cart.Clear();
        UpdateTotals();
    }

    // --- Custom Item ---
    [ObservableProperty] private bool _isCustomItemDialogOpen;
    [ObservableProperty] private string _customItemName = string.Empty;
    [ObservableProperty] private decimal _customItemPrice;

    [RelayCommand]
    private void OpenCustomItemDialog()
    {
        CustomItemName = "Misc Item";
        CustomItemPrice = 0;
        IsCustomItemDialogOpen = true;
    }

    [RelayCommand]
    private void AddCustomItem()
    {
        if (string.IsNullOrWhiteSpace(CustomItemName) || CustomItemPrice < 0) return;
        var dummyProduct = new Product
        {
            ProductId = -DateTime.Now.Millisecond,
            Name = CustomItemName,
            Price = CustomItemPrice,
            Stock = 999
        };
        Cart.Add(new CartItemViewModel(dummyProduct, this));
        UpdateTotals();
        IsCustomItemDialogOpen = false;
    }

    // --- Checkout ---
    [ObservableProperty] private bool _isCheckoutDialogOpen;
    [ObservableProperty] private decimal _cashTendered;
    [ObservableProperty] private decimal _changeDue;

    [RelayCommand]
    private void OpenCheckoutDialog()
    {
        if (Cart.Count == 0 || _auth.CurrentUser == null) return;
        IsKhataEnabled = false;
        SelectedCustomer = null;
        CashTendered = GrandTotal;
        IsCheckoutDialogOpen = true;
    }

    // --- Khata ---
    [ObservableProperty] private bool _isKhataEnabled;
    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private bool _isNewCustomerDialogOpen;
    [ObservableProperty] private string _newCustomerName = string.Empty;
    [ObservableProperty] private string _newCustomerPhone = string.Empty;

    partial void OnIsKhataEnabledChanged(bool value)
    {
        if (!value)
        {
            SelectedCustomer = null;
            CashTendered = GrandTotal;
        }
        else
        {
            CashTendered = 0;
        }
        UpdateChangeDue();
    }

    partial void OnSelectedCustomerChanged(Customer? value)
    {
        UpdateChangeDue();
    }

    [RelayCommand]
    private void OpenNewCustomerDialog()
    {
        NewCustomerName = string.Empty;
        NewCustomerPhone = string.Empty;
        IsNewCustomerDialogOpen = true;
    }

    [RelayCommand]
    private async Task SaveNewCustomer()
    {
        if (string.IsNullOrWhiteSpace(NewCustomerName)) return;
        var c = new Customer { Name = NewCustomerName, Phone = NewCustomerPhone };
        await _db.AddCustomerAsync(c);
        await LoadCustomersAsync();
        SelectedCustomer = CustomersList.FirstOrDefault(x => x.Name == NewCustomerName);
        IsNewCustomerDialogOpen = false;
    }

    // Arrears that WILL be added for this sale
    public decimal PendingArrears => IsKhataEnabled
        ? Math.Max(0, GrandTotal - CashTendered)
        : 0;

    private void UpdateChangeDue()
    {
        if (IsKhataEnabled)
            ChangeDue = 0; // no change due on khata; extra cash already paid
        else
            ChangeDue = CashTendered - GrandTotal;
        OnPropertyChanged(nameof(PendingArrears));
    }

    // --- Receipt ---
    [ObservableProperty] private bool _isReceiptDialogOpen;
    [ObservableProperty] private string _receiptText = string.Empty;

    [RelayCommand]
    private void AddFastCash(string amountStr)
    {
        if (decimal.TryParse(amountStr, out var amount))
        {
            CashTendered += amount;
        }
    }

    partial void OnCashTenderedChanged(decimal value) => UpdateChangeDue();

    [RelayCommand]
    private async Task FinalizeCheckout()
    {
        if (Cart.Count == 0 || _auth.CurrentUser == null) return;
        if (!IsKhataEnabled && CashTendered < GrandTotal) return;
        if (IsKhataEnabled && SelectedCustomer == null) return; // must select a customer for khata

        string invoiceNum = "INV" + DateTime.Now.ToString("yyyyMMddHHmmss");

        var sale = new Sale
        {
            InvoiceNumber = invoiceNum,
            SaleDate = DateTime.Now,
            StoreId = _auth.CurrentUser.StoreId ?? 1,
            UserId = _auth.CurrentUser.UserId,
            Subtotal = SubTotal,
            Tax = Tax,
            GrandTotal = GrandTotal,
            CashPaid = IsKhataEnabled ? CashTendered : GrandTotal,
            CustomerId = SelectedCustomer?.CustomerId,
            SaleItems = Cart.Select(c => new SaleItem
            {
                ProductId = c.Product.ProductId > 0 ? c.Product.ProductId : null,
                ProductName = c.Product.UrduName ?? c.Product.Name,
                Quantity = c.Quantity,
                UnitPrice = c.Price,
                Subtotal = c.TotalPrice
            }).ToList()
        };

        await _db.AddSaleAsync(sale);
        IsCheckoutDialogOpen = false;

        // Build Receipt - REALISTIC THERMAL PRINTER FORMAT
        var store = _auth.CurrentUser.Store;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"      {store?.Name ?? "WAQAR PHOTOSTATE"}      ");
        if (store?.UrduTagline != null) sb.AppendLine($"    {store.UrduTagline}     ");
        sb.AppendLine($"    {store?.Address ?? "Main Market, Lahore"}     ");
        sb.AppendLine($"       Ph: {store?.ContactNumber ?? "0300-1234567"}     ");
        sb.AppendLine("============================");
        sb.AppendLine($"Invoice: {invoiceNum}");
        sb.AppendLine($"Date:    {sale.SaleDate:g}");
        sb.AppendLine($"Cashier: {_auth.CurrentUser.Username}");
        if (SelectedCustomer != null)
            sb.AppendLine($"Account: {SelectedCustomer.Name}");
        sb.AppendLine("----------------------------");
        sb.AppendLine("ITEMS             QTY   AMT ");
        foreach (var item in sale.SaleItems)
        {
            string namePart = item.ProductName.Length > 15 ? item.ProductName.Substring(0, 15) : item.ProductName.PadRight(15);
            sb.AppendLine($"{namePart} {item.Quantity,3} {item.Subtotal,6:N0}");
        }
        sb.AppendLine("----------------------------");
        sb.AppendLine($"SUBTOTAL:         Rs {SubTotal,7:N0}");
        if (IsWholesaleMode) sb.AppendLine("MODE:             WHOLESALE ");
        sb.AppendLine($"TAX (0%):         Rs {Tax,7:N0}");
        sb.AppendLine("============================");
        sb.AppendLine($"GRAND TOTAL:      Rs {GrandTotal,7:N0}");
        sb.AppendLine("----------------------------");
        sb.AppendLine($"CASH PAID:        Rs {sale.CashPaid,7:N0}");
        
        if (PendingArrears > 0)
        {
            sb.AppendLine($"BALANCE (UDHAAR): Rs {PendingArrears,7:N0}");
            if (SelectedCustomer != null)
                sb.AppendLine($"TOTAL DEBT:       Rs {SelectedCustomer.ArrearsBalance + PendingArrears,7:N0}");
        }
        else
        {
            sb.AppendLine($"CHANGE DUE:       Rs {ChangeDue,7:N0}");
        }

        sb.AppendLine("============================");
        sb.AppendLine("  Shukriya! Phir Aain  ");
        sb.AppendLine("   No Return Policy    ");
        sb.AppendLine("     Software By AHMED ");
        
        ReceiptText = sb.ToString();
        IsReceiptDialogOpen = true;

        ClearCart();
        await LoadProductsAsync();
        await LoadCustomersAsync();
    }

    [RelayCommand]
    private void CloseReceiptDialog() => IsReceiptDialogOpen = false;

    [RelayCommand]
    private void Logout()
    {
        _auth.Logout();
        _nav.NavigateTo<LoginViewModel>();
    }

    public void UpdateTotals()
    {
        SubTotal = Cart.Sum(c => c.TotalPrice);
        Tax = 0;
        GrandTotal = SubTotal + Tax;
        UpdateChangeDue();
    }
}

public partial class CartItemViewModel : ViewModelBase
{
    private readonly PosViewModel _parent;

    [ObservableProperty] private Product _product;
    [ObservableProperty] private int _quantity;
    [ObservableProperty] private decimal _price;

    public decimal TotalPrice => Price * Quantity;

    public CartItemViewModel(Product product, PosViewModel parent)
    {
        _parent = parent;
        Product = product;
        Quantity = 1;
        RefreshPrice();
    }

    public void RefreshPrice()
    {
        Price = _parent.IsWholesaleMode ? Product.WholesalePrice : Product.Price;
        if (Price == 0) Price = Product.Price; // Fallback if wholesale is not set
        OnPropertyChanged(nameof(TotalPrice));
    }

    [RelayCommand]
    private void IncreaseQuantity()
    {
        Quantity++;
        OnPropertyChanged(nameof(TotalPrice));
        _parent.UpdateTotals();
    }

    [RelayCommand]
    private void DecreaseQuantity()
    {
        if (Quantity > 1)
        {
            Quantity--;
            OnPropertyChanged(nameof(TotalPrice));
            _parent.UpdateTotals();
        }
    }
}
