using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MzansiMarket.Data;
using MzansiMarket.Models;
using MzansiMarket.Dtos;
using MzansiMarket.Services;
using System.Globalization;

var culture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers(); 
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

const string CorsPolicy = "AllowFrontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, p =>
        p.AllowAnyOrigin()      // 
         .AllowAnyHeader()
         .AllowAnyMethod());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();

var app = builder.Build();

app.UseCors(CorsPolicy);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/auth/register", async ([FromBody] RegisterUserDto dto, AppDbContext db, IPasswordHasher hasher) =>
{
    if (await db.Users.AnyAsync(u => u.Username == dto.Username || u.Email == dto.Email))
        return Results.BadRequest("Username or Email already exists.");

    var (hash, salt) = hasher.Hash(dto.Password);

    var user = new User
    {
        Username = dto.Username.Trim(),
        Email = dto.Email.Trim().ToLowerInvariant(),
        PasswordHash = hash,
        PasswordSalt = salt
    };
    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"/users/{user.UserId}", new { user.UserId, user.Username, user.Email });
});

app.MapPost("/auth/login", async ([FromBody] LoginDto dto, AppDbContext db, IPasswordHasher hasher) =>
{
    var user = await db.Users.SingleOrDefaultAsync(u => u.Username == dto.Username || u.Email == dto.Username);
    
    if (user is null) return Results.Unauthorized();

    if (!hasher.Verify(dto.Password, user.PasswordHash, user.PasswordSalt))
        return Results.BadRequest("Invalid credentials.");

    return Results.Ok(new { token = $"demo-token-{user.UserId}", user = new { user.UserId, user.Username, user.Email } });
});

app.MapGet("/vendors", async ([FromQuery] string? q, AppDbContext db) =>
{
    var vendors = db.Vendors.AsQueryable();
    if (!string.IsNullOrWhiteSpace(q))
        vendors = vendors.Where(v => v.VendorName.Contains(q));

    var list = await vendors
        .OrderBy(v => v.VendorName)
        .Select(v => new VendorSummaryDto
        {
            VendorId = v.VendorId,
            VendorName = v.VendorName,
            CompanyReg = v.CompanyReg,
            ProductCount = v.Products.Count
        })
        .ToListAsync();

    return Results.Ok(list);
});

app.MapGet("/vendors/{id:int}", async (int id, AppDbContext db) =>
{
    var vendor = await db.Vendors.Include(v => v.Products).FirstOrDefaultAsync(v => v.VendorId == id);
    return vendor is null ? Results.NotFound() : Results.Ok(vendor);
});

app.MapPost("/vendors", async ([FromBody] CreateVendorDto dto, AppDbContext db) =>
{
    if (await db.Vendors.AnyAsync(v => v.CompanyReg == dto.CompanyReg))
        return Results.BadRequest("Vendor with this CompanyReg already exists.");

    var vendor = new Vendor { VendorName = dto.VendorName.Trim(), CompanyReg = dto.CompanyReg.Trim() };
    db.Vendors.Add(vendor);
    await db.SaveChangesAsync();
    return Results.Created($"/vendors/{vendor.VendorId}", vendor);
});

app.MapPut("/vendors/{id:int}", async (int id, [FromBody] UpdateVendorDto dto, AppDbContext db) =>
{
    var vendor = await db.Vendors.FindAsync(id);
    if (vendor is null) return Results.NotFound();

    vendor.VendorName = dto.VendorName?.Trim() ?? vendor.VendorName;
    vendor.CompanyReg = dto.CompanyReg?.Trim() ?? vendor.CompanyReg;

    await db.SaveChangesAsync();
    return Results.Ok(vendor);
});

app.MapDelete("/vendors/{id:int}", async (int id, AppDbContext db) =>
{
    var vendor = await db.Vendors.FindAsync(id);
    if (vendor is null) return Results.NotFound();

    db.Vendors.Remove(vendor);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapGet("/products", async (
    [FromQuery] int? vendorId,
    [FromQuery] string? location,
    [FromQuery] decimal? minPrice,
    [FromQuery] decimal? maxPrice,
    [FromQuery] string? search,
    AppDbContext db) =>
{
    var q = db.Products.Include(p => p.Vendor).AsQueryable();

    if (vendorId.HasValue) q = q.Where(p => p.VendorId == vendorId.Value);
    if (!string.IsNullOrWhiteSpace(location)) q = q.Where(p => p.Location == location);
    if (minPrice.HasValue) q = q.Where(p => p.Price >= minPrice.Value);
    if (maxPrice.HasValue) q = q.Where(p => p.Price <= maxPrice.Value);
    if (!string.IsNullOrWhiteSpace(search))
        q = q.Where(p => p.Description!.Contains(search) || p.Vendor!.VendorName.Contains(search));

    var list = await q.OrderBy(p => p.Price)
        .Select(p => new ProductDto
        {
            ProductId = p.ProductId,
            VendorId = p.VendorId,
            VendorName = p.Vendor!.VendorName,
            Stock = p.Stock,
            Price = p.Price,
            Location = p.Location,
            Description = p.Description
        }).ToListAsync();

    return Results.Ok(list);
});

app.MapGet("/products/{id:int}", async (int id, AppDbContext db) =>
{
    var p = await db.Products.Include(x => x.Vendor).FirstOrDefaultAsync(x => x.ProductId == id);
    return p is null ? Results.NotFound() : Results.Ok(new ProductDto
    {
        ProductId = p.ProductId,
        VendorId = p.VendorId,
        VendorName = p.Vendor!.VendorName,
        Stock = p.Stock,
        Price = p.Price,
        Location = p.Location,
        Description = p.Description
    });
});

app.MapPost("/products", async ([FromBody] CreateProductDto dto, AppDbContext db) =>
{
    if (!await db.Vendors.AnyAsync(v => v.VendorId == dto.VendorId))
        return Results.BadRequest("Vendor not found.");

    var product = new Product
    {
        VendorId = dto.VendorId,
        Stock = dto.Stock,
        Price = dto.Price,
        Location = dto.Location.Trim(),
        Description = dto.Description
    };

    db.Products.Add(product);
    await db.SaveChangesAsync();
    return Results.Created($"/products/{product.ProductId}", product);
});

app.MapPut("/products/{id:int}", async (int id, [FromBody] UpdateProductDto dto, AppDbContext db) =>
{
    var p = await db.Products.FindAsync(id);
    if (p is null) return Results.NotFound();

    if (dto.VendorId.HasValue)
    {
        var vendorExists = await db.Vendors.AnyAsync(v => v.VendorId == dto.VendorId.Value);
        if (!vendorExists) return Results.BadRequest("Vendor not found.");
        p.VendorId = dto.VendorId.Value;
    }

    if (dto.Stock.HasValue) p.Stock = dto.Stock;
    if (dto.Price.HasValue) p.Price = dto.Price.Value;
    if (!string.IsNullOrWhiteSpace(dto.Location)) p.Location = dto.Location.Trim();
    if (dto.DescriptionSet) p.Description = dto.Description; // allow null reset

    await db.SaveChangesAsync();
    return Results.Ok(p);
});

app.MapDelete("/products/{id:int}", async (int id, AppDbContext db) =>
{
    var p = await db.Products.FindAsync(id);
    if (p is null) return Results.NotFound();

    db.Products.Remove(p);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapPost("/purchases", async ([FromBody] PurchaseRequestDto dto, AppDbContext db) =>
{
    var user = await db.Users.FindAsync(dto.UserId);
    if (user is null) return Results.BadRequest("User not found.");

    var product = await db.Products.FindAsync(dto.ProductId);
    if (product is null) return Results.BadRequest("Product not found.");

    if (dto.Quantity <= 0) return Results.BadRequest("Quantity must be positive.");

    if (product.Stock.HasValue && product.Stock.Value < dto.Quantity)
        return Results.BadRequest("Insufficient stock.");

    if (product.Stock.HasValue)
        product.Stock -= dto.Quantity;

    var purchase = new Purchase
    {
        UserId = user.UserId,
        ProductId = product.ProductId,
        Quantity = dto.Quantity,
        UnitPrice = product.Price,
        TotalPrice = product.Price * dto.Quantity,
        CreatedAt = DateTimeOffset.UtcNow
    };

    db.Purchases.Add(purchase);
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        purchase.PurchaseId,
        purchase.UserId,
        purchase.ProductId,
        purchase.Quantity,
        purchase.UnitPrice,
        purchase.TotalPrice,
        purchase.CreatedAt
    });
});

app.MapGet("/users/{userId:int}/purchases", async (int userId, AppDbContext db) =>
{
    var items = await db.Purchases
        .Include(p => p.Product)
        .ThenInclude(pr => pr!.Vendor)
        .Where(p => p.UserId == userId)
        .OrderByDescending(p => p.CreatedAt)
        .Select(p => new
        {
            p.PurchaseId,
            p.CreatedAt,
            p.Quantity,
            p.UnitPrice,
            p.TotalPrice,
            Product = new { p.ProductId, p.Product!.Description, p.Product.Location, Vendor = p.Product.Vendor!.VendorName }
        }).ToListAsync();

    return Results.Ok(items);
});

app.UseHttpsRedirection();
app.MapControllers(); 

app.Run();