using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using SmartTeam.Application.Services;
using SmartTeam.Application.DTOs;
using FluentValidation;
using System.Reflection;

namespace SmartTeam.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Add AutoMapper
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        // Add FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Add Application Services
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IFilterService, FilterService>();
        services.AddScoped<IBannerService, BannerService>();
        services.AddScoped<IFavoriteService, FavoriteService>();
        services.AddScoped<IDownloadableFileService, DownloadableFileService>();
        services.AddScoped<IProductPdfService, ProductPdfService>();
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<IPromoCodeService, PromoCodeService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ILoyaltyService, LoyaltyService>();
        services.AddScoped<IInstallmentService, InstallmentService>();
        services.AddScoped<IQuizService, QuizService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<ICreditRequestService, CreditRequestService>();
        services.AddScoped<IExpargoService, ExpargoService>();
        
        // Add HttpClient for EpointService
        services.AddHttpClient<IEpointService, EpointService>();

        // Add HttpClient for AzerpostService
        services.AddHttpClient<IAzerpostService, AzerpostService>();

        return services;
    }
}
