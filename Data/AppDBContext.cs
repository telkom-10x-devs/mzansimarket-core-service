namespace MzansiMarket.Data
{
    using Microsoft.EntityFrameworkCore;
    using MzansiMarket.Models;

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Vendor> Vendors => Set<Vendor>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Purchase> Purchases => Set<Purchase>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            // Users
            b.Entity<User>(e =>
            {
                e.HasKey(x => x.UserId);
                e.HasIndex(x => x.Username).IsUnique();
                e.HasIndex(x => x.Email).IsUnique();
                e.Property(x => x.Username).IsRequired().HasMaxLength(50);
                e.Property(x => x.Email).IsRequired().HasMaxLength(200);
                e.Property(x => x.PasswordHash).IsRequired();
                e.Property(x => x.PasswordSalt).IsRequired();
            });

            // Vendors
            b.Entity<Vendor>(e =>
            {
                e.HasKey(x => x.VendorId);
                e.HasIndex(x => x.CompanyReg).IsUnique();
                e.Property(x => x.VendorName).IsRequired().HasMaxLength(120);
                e.Property(x => x.CompanyReg).IsRequired().HasMaxLength(60);
                e.HasMany(x => x.Products)
                 .WithOne(p => p.Vendor)
                 .HasForeignKey(p => p.VendorId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // Products
            b.Entity<Product>(e =>
            {
                e.HasKey(x => x.ProductId);
                e.Property(x => x.Price).HasPrecision(18, 2);
                e.Property(x => x.Location).IsRequired().HasMaxLength(120);
                e.Property(x => x.Description).HasMaxLength(2048);
            });

            // Purchases
            b.Entity<Purchase>(e =>
            {
                e.HasKey(x => x.PurchaseId);
                e.Property(x => x.UnitPrice).HasPrecision(18, 2);
                e.Property(x => x.TotalPrice).HasPrecision(18, 2);
                e.HasOne(x => x.Product)
                 .WithMany()
                 .HasForeignKey(x => x.ProductId)
                 .OnDelete(DeleteBehavior.Restrict);
                e.HasOne<User>()
                 .WithMany()
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}