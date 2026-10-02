using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Registry.Application.Accounts;
using Registry.Application.Common;
using Registry.Infrastructure.Identity;
using Registry.Infrastructure.Identity.External;
using Registry.Infrastructure.Persistence;

namespace Registry.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("Registry")));
        services.AddScoped<IRegistryDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IUserDirectory, UserDirectory>();

        services
            .AddIdentityApiEndpoints<User>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddTransient<IEmailSender<User>, LoggingEmailSender>();
        services.AddScoped<ExternalLoginService>();

        services.Configure<ExternalAuthOptions>(configuration.GetSection(ExternalAuthOptions.SectionName));
        services.AddKeyedSingleton<IExternalTokenValidator, GoogleTokenValidator>(ExternalProviders.Google);
        services.AddKeyedSingleton<IExternalTokenValidator, MicrosoftTokenValidator>(ExternalProviders.Microsoft);
        services.AddHttpClient<FacebookTokenValidator>(client =>
            client.BaseAddress = new Uri("https://graph.facebook.com/v21.0/"));
        services.AddKeyedTransient<IExternalTokenValidator>(
            ExternalProviders.Facebook,
            (sp, _) => sp.GetRequiredService<FacebookTokenValidator>());

        return services;
    }
}
