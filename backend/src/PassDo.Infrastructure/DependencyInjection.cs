using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Common.Options;
using PassDo.Application.Locations;
using PassDo.Domain.Constants;
using PassDo.Infrastructure.Health;
using PassDo.Infrastructure.Identity;
using PassDo.Infrastructure.Options;
using PassDo.Infrastructure.Presence;
using PassDo.Infrastructure.Persistence;
using PassDo.Infrastructure.Services;
using PassDo.Infrastructure.Storage;

namespace PassDo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        services.Configure<ShippingOptions>(configuration.GetSection(ShippingOptions.SectionName));
        services.Configure<GoogleAuthOptions>(configuration.GetSection(GoogleAuthOptions.SectionName));

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<PassDoDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(PassDoDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<PassDoDbContext>());
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IPresenceTracker, PresenceTracker>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasherService>();
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationRealtimePublisher, NullNotificationRealtimePublisher>();
        services.AddSingleton<IShippingCalculator, ShippingCalculator>();
        services.AddSingleton<LocalFileStorageService>();
        services.AddSingleton<IFileStorageService>(provider => provider.GetRequiredService<LocalFileStorageService>());

        services.AddMemoryCache();
        services.AddHttpClient<IVietnamLocationService, VietnamLocationService>(client =>
        {
            client.BaseAddress = new Uri("https://provinces.open-api.vn/api/");
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddHttpContextAccessor();

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration is missing.");

        if (string.IsNullOrWhiteSpace(jwtOptions.Key) || jwtOptions.Key.Length < 32)
        {
            throw new InvalidOperationException("JWT key must be configured and at least 32 characters long.");
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole(Roles.Admin));
        });

        services.AddHealthChecks()
            .AddDbContextCheck<PassDoDbContext>("database")
            .AddCheck<FileStorageHealthCheck>("file-storage");

        return services;
    }
}
