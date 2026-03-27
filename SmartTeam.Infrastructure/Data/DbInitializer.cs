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

        // Seed quiz questions if not already seeded
        await SeedQuizQuestionsAsync(context);
    }

    private static async Task EnsureAdminUserAsync(SmartTeamDbContext context)
    {
        var adminEmail = "admin@gunaybeauty.az";
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

    private static async Task SeedQuizQuestionsAsync(SmartTeamDbContext context)
    {
        // Only seed if no questions exist yet
        if (await context.QuizQuestions.AnyAsync())
            return;

        var q1 = new QuizQuestion
        {
            Id = Guid.Parse("11111111-0000-0000-0000-000000000001"),
            QuestionText = "Günortada nəmləndiricisiz dəriniz necə hiss edir?",
            StepKey = "SkinType",
            SortOrder = 1,
            CreatedAt = DateTime.UtcNow,
            AnswerOptions = new List<QuizAnswerOption>
            {
                new() { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"), AnswerCode = "ST1", AnswerText = "Dəri Tipi 1: Quru", SubText = "Dəriniz hər yerdə sıx, narahat və quru hiss edir.", SortOrder = 1 },
                new() { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002"), AnswerCode = "ST2", AnswerText = "Dəri Tipi 2: Quru Kombinasiya", SubText = "Yanaqlarınız sıx hiss edir, T-zona isə rahat hiss edir.", SortOrder = 2 },
                new() { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003"), AnswerCode = "ST3", AnswerText = "Dəri Tipi 3: Yağlı Kombinasiya", SubText = "Dəriniz yanaqlarında rahat, T-zonada yağlı hiss edir. Bəzən dəriniz tamamilə rahat hiss edir.", SortOrder = 3 },
                new() { Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000004"), AnswerCode = "ST4", AnswerText = "Dəri Tipi 4: Yağlı", SubText = "Dəriniz hər yerdə yağlı və ya parlaq görünür, məsamələrin tıxanmasına meyllidir.", SortOrder = 4 }
            }
        };

        var q2 = new QuizQuestion
        {
            Id = Guid.Parse("11111111-0000-0000-0000-000000000002"),
            QuestionText = "Bu gün üzünüzün əsas dəri narahatlığı nədir?",
            StepKey = "SkinConcern",
            SortOrder = 2,
            CreatedAt = DateTime.UtcNow,
            AnswerOptions = new List<QuizAnswerOption>
            {
                new() { Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001"), AnswerCode = "SC1", AnswerText = "Sızanaqlar", SortOrder = 1 },
                new() { Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002"), AnswerCode = "SC2", AnswerText = "Hiperpiqmentasiya və ya Qaranlıq Ləkələr", SortOrder = 2 },
                new() { Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000003"), AnswerCode = "SC3", AnswerText = "Göz Altı Qaranlıq Dairələr", SortOrder = 3 },
                new() { Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000004"), AnswerCode = "SC4", AnswerText = "Xətlər və Qırışlar", SortOrder = 4 },
                new() { Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000005"), AnswerCode = "SC5", AnswerText = "Quru və ya Dehidratasiyalı Dəri", SortOrder = 5 },
                new() { Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000006"), AnswerCode = "SC6", AnswerText = "Kişi Dərisi", SortOrder = 6 },
                new() { Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000007"), AnswerCode = "SC7", AnswerText = "Qeyri-bərabər Toxuma və Görünən Məsamələr", SortOrder = 7 },
                new() { Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000008"), AnswerCode = "SC8", AnswerText = "Qızartı və Görünən Həssaslıq", SortOrder = 8 }
            }
        };

        var q3 = new QuizQuestion
        {
            Id = Guid.Parse("11111111-0000-0000-0000-000000000003"),
            QuestionText = "Gündəlik günəş mühafizəsi baxımından sizin üçün ən vacib olan nədir?",
            StepKey = "SpfPreference",
            SortOrder = 3,
            CreatedAt = DateTime.UtcNow,
            AnswerOptions = new List<QuizAnswerOption>
            {
                new() { Id = Guid.Parse("cccccccc-0000-0000-0000-000000000001"), AnswerCode = "SP1", AnswerText = "Nəmləndiricimə və ya fondantıma daxil edilmiş", SortOrder = 1 },
                new() { Id = Guid.Parse("cccccccc-0000-0000-0000-000000000002"), AnswerCode = "SP2", AnswerText = "100% mineral əsaslı", SortOrder = 2 },
                new() { Id = Guid.Parse("cccccccc-0000-0000-0000-000000000003"), AnswerCode = "SP3", AnswerText = "Nəmləndiricimin üzərindən və ya makiyaj altında UV mühafizə bazası", SortOrder = 3 },
                new() { Id = Guid.Parse("cccccccc-0000-0000-0000-000000000004"), AnswerCode = "SP4", AnswerText = "Yüksək SPF", SortOrder = 4 }
            }
        };

        await context.QuizQuestions.AddRangeAsync(q1, q2, q3);
        await context.SaveChangesAsync();
    }
}
