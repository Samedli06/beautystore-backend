using SmartTeam.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace SmartTeam.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(SmartTeamDbContext context)
    {
        // Database creation and migration is now handled by Program.cs
        
        // Always ensure admin user exists with correct credentials
        await EnsureAdminUserAsync(context);

        // Only seed initial data if no users exist (excluding the admin we just created/updated)
        var userCount = await context.Users.CountAsync();
        if (userCount > 1)
        {
            return; // Database has been seeded with more than just admin
        }
        var passwordHasher = new PasswordHasher<User>();
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Admin",
            LastName = "User",
            Email = "admin@gunaybeauty.az",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "Admin123!");
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "User",
            Email = "test@gunaybeauty.az",
            Role = UserRole.NormalUser,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        testUser.PasswordHash = passwordHasher.HashPassword(testUser, "Test123!");

        await context.Users.AddRangeAsync(adminUser, testUser);
        var electronics = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Electronics",
            Slug = "electronics",
            Description = "Electronic devices and gadgets",
            IsActive = true,
            SortOrder = 1,
            CreatedAt = DateTime.UtcNow
        };

        var clothing = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Clothing",
            Slug = "clothing",
            Description = "Fashion and apparel",
            IsActive = true,
            SortOrder = 2,
            CreatedAt = DateTime.UtcNow
        };

        await context.Categories.AddRangeAsync(electronics, clothing);

        // Seed Filters
        var brandFilter = new Filter
        {
            Id = Guid.NewGuid(),
            Name = "Brand",
            Slug = "brand",
            Type = FilterType.Select,
            IsActive = true,
            SortOrder = 1,
            CreatedAt = DateTime.UtcNow
        };

        var sizeFilter = new Filter
        {
            Id = Guid.NewGuid(),
            Name = "Size",
            Slug = "size",
            Type = FilterType.Select,
            IsActive = true,
            SortOrder = 2,
            CreatedAt = DateTime.UtcNow
        };

        var priceFilter = new Filter
        {
            Id = Guid.NewGuid(),
            Name = "Price Range",
            Slug = "price-range",
            Type = FilterType.Range,
            IsActive = true,
            SortOrder = 3,
            CreatedAt = DateTime.UtcNow
        };

        await context.Filters.AddRangeAsync(brandFilter, sizeFilter, priceFilter);

        // Seed Filter Options
        var brandOptions = new[]
        {
            new FilterOption { Id = Guid.NewGuid(), FilterId = brandFilter.Id, Value = "apple", DisplayName = "Apple", IsActive = true, SortOrder = 1, CreatedAt = DateTime.UtcNow },
            new FilterOption { Id = Guid.NewGuid(), FilterId = brandFilter.Id, Value = "samsung", DisplayName = "Samsung", IsActive = true, SortOrder = 2, CreatedAt = DateTime.UtcNow },
            new FilterOption { Id = Guid.NewGuid(), FilterId = brandFilter.Id, Value = "nike", DisplayName = "Nike", IsActive = true, SortOrder = 3, CreatedAt = DateTime.UtcNow }
        };

        var sizeOptions = new[]
        {
            new FilterOption { Id = Guid.NewGuid(), FilterId = sizeFilter.Id, Value = "s", DisplayName = "Small", IsActive = true, SortOrder = 1, CreatedAt = DateTime.UtcNow },
            new FilterOption { Id = Guid.NewGuid(), FilterId = sizeFilter.Id, Value = "m", DisplayName = "Medium", IsActive = true, SortOrder = 2, CreatedAt = DateTime.UtcNow },
            new FilterOption { Id = Guid.NewGuid(), FilterId = sizeFilter.Id, Value = "l", DisplayName = "Large", IsActive = true, SortOrder = 3, CreatedAt = DateTime.UtcNow }
        };

        await context.FilterOptions.AddRangeAsync(brandOptions);
        await context.FilterOptions.AddRangeAsync(sizeOptions);

        // Seed Sample Products
        var product1 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "iPhone 15 Pro",
            Slug = "iphone-15-pro",
            Description = "Latest iPhone with advanced features",
            ShortDescription = "Premium smartphone",
            Sku = "IPH15PRO001",
            CategoryId = electronics.Id,
            IsActive = true,
            IsHotDeal = true,
            StockQuantity = 50,
            CreatedAt = DateTime.UtcNow
        };

        var product2 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Nike Air Max",
            Slug = "nike-air-max",
            Description = "Comfortable running shoes",
            ShortDescription = "Sports shoes",
            Sku = "NIKE001",
            CategoryId = clothing.Id,
            IsActive = true,
            IsHotDeal = false,
            StockQuantity = 100,
            CreatedAt = DateTime.UtcNow
        };

        await context.Products.AddRangeAsync(product1, product2);

        // Seed Product Prices (Role-based)
        var product1Prices = new[]
        {
            new ProductPrice { Id = Guid.NewGuid(), ProductId = product1.Id, UserRole = UserRole.NormalUser, Price = 1300m, CreatedAt = DateTime.UtcNow },
            new ProductPrice { Id = Guid.NewGuid(), ProductId = product1.Id, UserRole = UserRole.Retail, Price = 1200m, CreatedAt = DateTime.UtcNow },
            new ProductPrice { Id = Guid.NewGuid(), ProductId = product1.Id, UserRole = UserRole.Wholesale, Price = 1000m, CreatedAt = DateTime.UtcNow },
            new ProductPrice { Id = Guid.NewGuid(), ProductId = product1.Id, UserRole = UserRole.VIP, Price = 950m, DiscountedPrice = 900m, DiscountPercentage = 5.26m, CreatedAt = DateTime.UtcNow }
        };

        var product2Prices = new[]
        {
            new ProductPrice { Id = Guid.NewGuid(), ProductId = product2.Id, UserRole = UserRole.NormalUser, Price = 180m, CreatedAt = DateTime.UtcNow },
            new ProductPrice { Id = Guid.NewGuid(), ProductId = product2.Id, UserRole = UserRole.Retail, Price = 150m, CreatedAt = DateTime.UtcNow },
            new ProductPrice { Id = Guid.NewGuid(), ProductId = product2.Id, UserRole = UserRole.Wholesale, Price = 120m, CreatedAt = DateTime.UtcNow },
            new ProductPrice { Id = Guid.NewGuid(), ProductId = product2.Id, UserRole = UserRole.VIP, Price = 100m, CreatedAt = DateTime.UtcNow }
        };

        await context.ProductPrices.AddRangeAsync(product1Prices);
        await context.ProductPrices.AddRangeAsync(product2Prices);

        // Seed Banner
        var heroBanner = new Banner
        {
            Id = Guid.NewGuid(),
            Title = "Welcome to GunayBeauty",
            ImageUrl = "/uploads/banners/c2148601-15d5-4d95-9c30-1541b6d668da.png",
            LinkUrl = "/",
            ButtonText = "Shop Now",
            Type = BannerType.Hero,
            IsActive = true,
            SortOrder = 1,
            CreatedAt = DateTime.UtcNow
        };

        await context.Banners.AddAsync(heroBanner);

        // Save all changes
        await context.SaveChangesAsync();
    }

    private static async Task EnsureAdminUserAsync(SmartTeamDbContext context)
    {
        var adminEmail = "admin@gunaybeauty.az";
        var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        var passwordHasher = new PasswordHasher<User>();

        if (adminUser == null)
        {
            // Create new admin user
            adminUser = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Admin",
                LastName = "User",
                Email = adminEmail,
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "Admin123!");
            await context.Users.AddAsync(adminUser);
            await context.SaveChangesAsync();
        }
        else
        {
            // Update existing admin user's password and ensure correct role
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "Admin123!");
            adminUser.Role = UserRole.Admin;
            adminUser.IsActive = true;
            adminUser.UpdatedAt = DateTime.UtcNow;
            context.Users.Update(adminUser);
            await context.SaveChangesAsync();
        }
    }
}
