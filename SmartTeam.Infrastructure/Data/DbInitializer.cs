using SmartTeam.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace SmartTeam.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(SmartTeamDbContext context)
    {
        // Always ensure admin user exists with correct credentials
        await EnsureAdminUserAsync(context);

    }

    private static async Task EnsureAdminUserAsync(SmartTeamDbContext context)
    {
        var adminEmail = "admin@avto027.com";
        var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        var passwordHasher = new PasswordHasher<User>();

        if (adminUser == null)
        {
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
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "Admin123!");
            adminUser.Role = UserRole.Admin;
            adminUser.IsActive = true;
            adminUser.UpdatedAt = DateTime.UtcNow;
            context.Users.Update(adminUser);
            await context.SaveChangesAsync();
        }
    }
}
