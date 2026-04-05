using Microsoft.EntityFrameworkCore;
using PosApp.Models;
using System;
using System.Security.Cryptography;
using System.Text;

namespace PosApp.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Store> Stores { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Sale> Sales { get; set; } = null!;
    public DbSet<SaleItem> SaleItems { get; set; } = null!;
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Expense> Expenses { get; set; } = null!;
    public DbSet<ArrearsPayment> ArrearsPayments { get; set; } = null!;

    public AppDbContext() { }
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=pos.db");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().HasOne(p => p.Store).WithMany(s => s.Products).HasForeignKey(p => p.StoreId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<User>().HasOne(u => u.Store).WithMany(s => s.Users).HasForeignKey(u => u.StoreId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Sale>().HasOne(s => s.Store).WithMany(st => st.Sales).HasForeignKey(s => s.StoreId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Sale>().HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Sale>().HasOne(s => s.Customer).WithMany(c => c.Sales).HasForeignKey(s => s.CustomerId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<ArrearsPayment>().HasOne(ap => ap.Customer).WithMany(c => c.ArrearsPayments).HasForeignKey(ap => ap.CustomerId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SaleItem>().HasOne(si => si.Sale).WithMany(s => s.SaleItems).HasForeignKey(si => si.SaleId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SaleItem>().HasOne(si => si.Product).WithMany().HasForeignKey(si => si.ProductId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);

        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }

        modelBuilder.Entity<Store>().HasData(
            new Store 
            { 
                StoreId = 1, 
                Name = "WAQAR PHOTOSTATE", 
                Location = "Main Market", 
                Address = "Shop #12, Alfalah Plaza, Main Market, Lahore",
                ContactNumber = "0300-1234567",
                UrduTagline = "بسم اللہ الرحمن الرحیم"
            }
        );

        modelBuilder.Entity<User>().HasData(
            new User { UserId = 1, Username = "admin", PasswordHash = ComputeHash("admin"), Role = "Admin", StoreId = 1 },
            new User { UserId = 2, Username = "cashier", PasswordHash = ComputeHash("cashier"), Role = "Cashier", StoreId = 1 }
        );

        modelBuilder.Entity<Product>().HasData(
            // --- Photocopy Services ---
            new Product { ProductId = 1, StoreId = 1, Name = "Photocopy B&W A4", UrduName = "فوٹو کاپی اے 4", Price = 5, WholesalePrice = 4, CostPrice = 1.2m, Stock = 10000, Category = "Photocopy", Barcode = "PC001" },
            new Product { ProductId = 2, StoreId = 1, Name = "Photocopy B&W Legal", UrduName = "فوٹو کاپی لیگل", Price = 10, WholesalePrice = 8, CostPrice = 2, Stock = 5000, Category = "Photocopy", Barcode = "PC002" },
            new Product { ProductId = 3, StoreId = 1, Name = "Photocopy B&W A3", UrduName = "فوٹو کاپی اے 3", Price = 25, WholesalePrice = 20, CostPrice = 5, Stock = 2000, Category = "Photocopy", Barcode = "PC003" },
            new Product { ProductId = 4, StoreId = 1, Name = "Color Copy A4 (Normal)", UrduName = "کلر کاپی اے 4 نارمل", Price = 30, WholesalePrice = 25, CostPrice = 8, Stock = 1000, Category = "Photocopy", Barcode = "PC004" },
            new Product { ProductId = 5, StoreId = 1, Name = "Color Copy A4 (Best)", UrduName = "کلر کاپی اے 4 بہترین", Price = 50, WholesalePrice = 40, CostPrice = 12, Stock = 1000, Category = "Photocopy", Barcode = "PC005" },
            new Product { ProductId = 6, StoreId = 1, Name = "ID Card / CNIC Copy", UrduName = "شناختی کارڈ کاپی", Price = 10, WholesalePrice = 7, CostPrice = 2.4m, Stock = 99999, Category = "Photocopy", Barcode = "PC006" },

            // --- Printing Services ---
            new Product { ProductId = 7, StoreId = 1, Name = "B&W Print A4", UrduName = "پرنٹ کالی سیاہی", Price = 10, WholesalePrice = 8, CostPrice = 1.5m, Stock = 10000, Category = "Printing", Barcode = "PR001" },
            new Product { ProductId = 8, StoreId = 1, Name = "Color Print A4", UrduName = "کلر پرنٹ اے 4", Price = 40, WholesalePrice = 35, CostPrice = 12, Stock = 2000, Category = "Printing", Barcode = "PR002" },
            new Product { ProductId = 9, StoreId = 1, Name = "Glossy Photo Print 4x6", UrduName = "تصویر پرنٹ 4x6", Price = 60, WholesalePrice = 50, CostPrice = 15, Stock = 500, Category = "Printing", Barcode = "PR003" },
            new Product { ProductId = 10, StoreId = 1, Name = "Glossy Photo Print A4", UrduName = "تصویر پرنٹ اے 4", Price = 150, WholesalePrice = 130, CostPrice = 40, Stock = 200, Category = "Printing", Barcode = "PR004" },
            new Product { ProductId = 11, StoreId = 1, Name = "CV / Resume Print", UrduName = "سی وی پرنٹ", Price = 50, WholesalePrice = 40, CostPrice = 5, Stock = 1000, Category = "Printing", Barcode = "PR005" },
            new Product { ProductId = 12, StoreId = 1, Name = "Admission Form Print", UrduName = "داخلہ فارم پرنٹ", Price = 30, WholesalePrice = 25, CostPrice = 3, Stock = 1000, Category = "Printing", Barcode = "PR006" },
            new Product { ProductId = 13, StoreId = 1, Name = "Card Printing (Ivory)", UrduName = "کارڈ پرنٹنگ", Price = 80, WholesalePrice = 70, CostPrice = 20, Stock = 300, Category = "Printing", Barcode = "PR007" },

            // --- Binding Services ---
            new Product { ProductId = 14, StoreId = 1, Name = "Spiral Binding (Normal)", UrduName = "سپرائل بائنڈنگ", Price = 80, WholesalePrice = 60, CostPrice = 25, Stock = 500, Category = "Binding", Barcode = "BD001" },
            new Product { ProductId = 15, StoreId = 1, Name = "Spiral Binding (Large)", UrduName = "بڑی بائنڈنگ", Price = 200, WholesalePrice = 180, CostPrice = 60, Stock = 100, Category = "Binding", Barcode = "BD002" },
            new Product { ProductId = 16, StoreId = 1, Name = "Tape Binding", UrduName = "ٹیپ بائنڈنگ", Price = 40, WholesalePrice = 30, CostPrice = 10, Stock = 1000, Category = "Binding", Barcode = "BD003" },
            new Product { ProductId = 17, StoreId = 1, Name = "Hard Case Project Binding", UrduName = "ہارڈ بائنڈنگ پروجیکٹ", Price = 650, WholesalePrice = 600, CostPrice = 300, Stock = 50, Category = "Binding", Barcode = "BD004" },
            new Product { ProductId = 18, StoreId = 1, Name = "Comb Binding", UrduName = "کومب بائنڈنگ", Price = 120, WholesalePrice = 100, CostPrice = 35, Stock = 200, Category = "Binding", Barcode = "BD005" },

            // --- Lamination & Finishing ---
            new Product { ProductId = 19, StoreId = 1, Name = "Lamination ID Card", UrduName = "لیمینیشن شناختی کارڈ", Price = 30, WholesalePrice = 20, CostPrice = 5, Stock = 1000, Category = "Finishing", Barcode = "FN001" },
            new Product { ProductId = 20, StoreId = 1, Name = "Lamination A4 / Legal", UrduName = "لیمینیشن اے 4", Price = 80, WholesalePrice = 60, CostPrice = 15, Stock = 500, Category = "Finishing", Barcode = "FN002" },

            // --- Digital Services ---
            new Product { ProductId = 21, StoreId = 1, Name = "Scanning A4 (1 Page)", UrduName = "سکیننگ", Price = 20, WholesalePrice = 15, CostPrice = 0.5m, Stock = 9999, Category = "Digital", Barcode = "DS001" },
            new Product { ProductId = 22, StoreId = 1, Name = "Scanning Bulk (Per Page)", UrduName = "سکیننگ بلک", Price = 10, WholesalePrice = 5, CostPrice = 0.2m, Stock = 9999, Category = "Digital", Barcode = "DS002" },
            new Product { ProductId = 23, StoreId = 1, Name = "CV Composing / Design", UrduName = "سی وی ڈیزائننگ", Price = 350, WholesalePrice = 300, CostPrice = 20, Stock = 999, Category = "Digital", Barcode = "DS003" },
            new Product { ProductId = 24, StoreId = 1, Name = "Online Job/Uni Form", UrduName = "آن لائن فارم", Price = 250, WholesalePrice = 200, CostPrice = 10, Stock = 999, Category = "Digital", Barcode = "DS004" },
            new Product { ProductId = 25, StoreId = 1, Name = "NADRA Online Token", UrduName = "نادرا ٹوکن", Price = 150, WholesalePrice = 140, CostPrice = 0, Stock = 999, Category = "Digital", Barcode = "DS005" },
            new Product { ProductId = 26, StoreId = 1, Name = "Passport Online App", UrduName = "پاسپورٹ فارم", Price = 500, WholesalePrice = 450, CostPrice = 0, Stock = 999, Category = "Digital", Barcode = "DS006" },
            new Product { ProductId = 27, StoreId = 1, Name = "Result Card Printing", UrduName = "رزلٹ کارڈ پرنٹ", Price = 50, WholesalePrice = 40, CostPrice = 5, Stock = 999, Category = "Digital", Barcode = "DS007" },
            new Product { ProductId = 28, StoreId = 1, Name = "Email / File Sending", UrduName = "ای میل سروس", Price = 30, WholesalePrice = 20, CostPrice = 0, Stock = 999, Category = "Digital", Barcode = "DS008" },
            new Product { ProductId = 29, StoreId = 1, Name = "Utility Bill Payment", UrduName = "بل ادائیگی", Price = 20, WholesalePrice = 10, CostPrice = 0, Stock = 999, Category = "Digital", Barcode = "DS009" },

            // --- Stationery & Supplies ---
            new Product { ProductId = 30, StoreId = 1, Name = "A4 Rim 70g (Local)", UrduName = "اے 4 کاغذ رم", Price = 1350, WholesalePrice = 1300, CostPrice = 1150, Stock = 12, Category = "Stationery", Barcode = "ST001" },
            new Product { ProductId = 31, StoreId = 1, Name = "A4 Rim 80g (DoubleA)", UrduName = "ڈیبل اے کاغذ رم", Price = 1800, WholesalePrice = 1750, CostPrice = 1580, Stock = 8, Category = "Stationery", Barcode = "ST002" },
            new Product { ProductId = 32, StoreId = 1, Name = "Legal Rim 75g", UrduName = "لیگل کاغذ رم", Price = 1650, WholesalePrice = 1600, CostPrice = 1420, Stock = 5, Category = "Stationery", Barcode = "ST003" },
            new Product { ProductId = 33, StoreId = 1, Name = "Uni-ball Eye (Blue)", UrduName = "یونی بال پین نیلا", Price = 250, WholesalePrice = 230, CostPrice = 190, Stock = 24, Category = "Stationery", Barcode = "ST004" },
            new Product { ProductId = 34, StoreId = 1, Name = "Uni-ball Eye (Black)", UrduName = "یونی بال پین کالا", Price = 250, WholesalePrice = 230, CostPrice = 190, Stock = 24, Category = "Stationery", Barcode = "ST005" },
            new Product { ProductId = 35, StoreId = 1, Name = "Sign Pen / Marker", UrduName = "سائن پین", Price = 30, WholesalePrice = 25, CostPrice = 18, Stock = 100, Category = "Stationery", Barcode = "ST006" },
            new Product { ProductId = 36, StoreId = 1, Name = "Clear File Folder", UrduName = "کلیر فائل", Price = 40, WholesalePrice = 30, CostPrice = 15, Stock = 200, Category = "Stationery", Barcode = "ST007" },
            new Product { ProductId = 37, StoreId = 1, Name = "Box File (Hard)", UrduName = "باکس فائل", Price = 150, WholesalePrice = 130, CostPrice = 85, Stock = 50, Category = "Stationery", Barcode = "ST008" },
            new Product { ProductId = 38, StoreId = 1, Name = "Plastic Envelope A4", UrduName = "لفافہ اے 4", Price = 15, WholesalePrice = 10, CostPrice = 7, Stock = 500, Category = "Stationery", Barcode = "ST009" },
            new Product { ProductId = 39, StoreId = 1, Name = "Glue Stick (Small)", UrduName = "گلو سٹک", Price = 60, WholesalePrice = 50, CostPrice = 35, Stock = 30, Category = "Stationery", Barcode = "ST010" },
            new Product { ProductId = 40, StoreId = 1, Name = "Scotch Tape (Small)", UrduName = "ٹیپ چھوٹی", Price = 25, WholesalePrice = 20, CostPrice = 12, Stock = 100, Category = "Stationery", Barcode = "ST011" },
            new Product { ProductId = 41, StoreId = 1, Name = "USB 32GB Kingst.", UrduName = "یو ایس بی 32 جی بی", Price = 950, WholesalePrice = 900, CostPrice = 720, Stock = 15, Category = "Stationery", Barcode = "ST012" },

            // --- Other Services ---
            new Product { ProductId = 42, StoreId = 1, Name = "Urgent Typing (Eng)", UrduName = "انگریزی ٹائپنگ", Price = 100, WholesalePrice = 80, CostPrice = 0, Stock = 999, Category = "Other", Barcode = "OT001" },
            new Product { ProductId = 43, StoreId = 1, Name = "Urgent Typing (Urdu)", UrduName = "اردو ٹائپنگ", Price = 200, WholesalePrice = 150, CostPrice = 0, Stock = 999, Category = "Other", Barcode = "OT002" },
            new Product { ProductId = 44, StoreId = 1, Name = "Computer Service Fee", UrduName = "کمپیوٹر سروس", Price = 100, WholesalePrice = 50, CostPrice = 0, Stock = 999, Category = "Other", Barcode = "OT003" }
        );

        modelBuilder.Entity<Expense>().HasData(
            new Expense { ExpenseId = 1, Description = "Shop Electricity Bill", Amount = 5400, Date = new DateTime(2026, 3, 22), Category = "Utility" },
            new Expense { ExpenseId = 2, Description = "Shop Internet Bill", Amount = 2500, Date = new DateTime(2026, 4, 1), Category = "Utility" },
            new Expense { ExpenseId = 3, Description = "Staff Tea (Weekly)", Amount = 1200, Date = new DateTime(2026, 4, 2), Category = "Daily" }
        );
    }
}
