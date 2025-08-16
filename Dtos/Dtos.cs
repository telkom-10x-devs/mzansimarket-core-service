namespace MzansiMarket.Dtos
{
    // Auth
    public record RegisterUserDto(string Username, string Email, string Password);
    public record LoginDto(string Username, string Password);

    // Vendors
    public class CreateVendorDto
    {
        public string VendorName { get; set; } = default!;
        public string CompanyReg { get; set; } = default!;
    }

    public class UpdateVendorDto
    {
        public string? VendorName { get; set; }
        public string? CompanyReg { get; set; }
    }

    public class VendorSummaryDto
    {
        public int VendorId { get; set; }
        public string VendorName { get; set; } = default!;
        public string CompanyReg { get; set; } = default!;
        public int ProductCount { get; set; }
    }

    // Products
    public class CreateProductDto
    {
        public int VendorId { get; set; }
        public int? Stock { get; set; }
        public decimal Price { get; set; }
        public string Location { get; set; } = default!;
        public string? Description { get; set; }
    }

    public class UpdateProductDto
    {
        public int? VendorId { get; set; }
        public int? Stock { get; set; }
        public decimal? Price { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
        public bool DescriptionSet { get; set; } // true if you want to set Description, even null
    }

    public class ProductDto
    {
        public int ProductId { get; set; }
        public int VendorId { get; set; }
        public string VendorName { get; set; } = default!;
        public int? Stock { get; set; }
        public decimal Price { get; set; }
        public string Location { get; set; } = default!;
        public string? Description { get; set; }
    }

    // Purchases
    public class PurchaseRequestDto
    {
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}