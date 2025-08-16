namespace MzansiMarket.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = default!;
        public string Email { get; set; } = default!;
        public byte[] PasswordHash { get; set; } = default!;
        public byte[] PasswordSalt { get; set; } = default!;
    }

    public class Vendor
    {
        public int VendorId { get; set; }
        public string VendorName { get; set; } = default!;
        public string CompanyReg { get; set; } = default!;

        public List<Product> Products { get; set; } = new();
    }

    public class Product
    {
        public int ProductId { get; set; }
        public int? Stock { get; set; }

        // Normalized FK instead of VendorName string on the entity
        public int VendorId { get; set; }
        public Vendor? Vendor { get; set; }

        public decimal Price { get; set; }
        public string Location { get; set; } = default!; // e.g., "Cape Town", "Johannesburg"
        public string? Description { get; set; }
    }

    public class Purchase
    {
        public int PurchaseId { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public Product? Product { get; set; }
    }
}
