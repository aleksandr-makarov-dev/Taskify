using System.Text;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Taskify.WebApi.Domain.Users;
using Taskify.WebApi.Infrastructure.BackgroundServices;
using Taskify.WebApi.Infrastructure.Filters;
using Taskify.WebApi.Infrastructure.Middlewares;
using Taskify.WebApi.Infrastructure.Options;
using Taskify.WebApi.Persistence;
using Taskify.WebApi.Persistence.Interceptors;
using Taskify.WebApi.Services;

namespace Taskify.WebApi.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddPersistence(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();

        services.AddDbContext<ApplicationDbContext>((provider, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

            options.AddInterceptors(
                provider.GetRequiredService<SoftDeleteInterceptor>(),
                provider.GetRequiredService<AuditInterceptor>());
        });
    }

    public static void AddIdentityAndAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddIdentityCore<User>(options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;

                options.SignIn.RequireConfirmedEmail = true;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddRoles<Role>()
            .AddSignInManager()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddSingleton<IJsonWebTokenService, JsonWebTokenService>();

        var jwtSection = configuration.GetSection(JsonWebTokenOptions.SectionName);
        var jwtOptions = jwtSection.Get<JsonWebTokenOptions>()
                         ?? throw new InvalidOperationException("JsonWebTokenOptions is not configured");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();
    }

    public static void AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddDataProtection();

        services.AddApplicationOptions();
        services.AddHostedServices();
        services.AddValidation();
        services.AddExceptionHandling();
        services.AddApiVersioningConfiguration();
    }

    private static void AddApplicationOptions(this IServiceCollection services)
    {
        services.AddOptions<JsonWebTokenOptions>()
            .BindConfiguration(JsonWebTokenOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<RefreshTokenOptions>()
            .BindConfiguration(RefreshTokenOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<ItemExpirationOptions>()
            .BindConfiguration(ItemExpirationOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SoftDeleteOptions>()
            .BindConfiguration(SoftDeleteOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    private static void AddHostedServices(this IServiceCollection services)
    {
        services.AddHostedService<ItemExpirationBackgroundService>();
        services.AddHostedService<SoftDeleteBackgroundService>();
    }

    private static void AddExceptionHandling(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = ctx =>
            {
                var httpContext = ctx.HttpContext;
                var problem = ctx.ProblemDetails;

                problem.Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}";
                problem.Extensions["timestamp"] = DateTimeOffset.UtcNow;
            };
        });

        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
    }

    private static void AddValidation(this IServiceCollection services)
    {
        services.AddScoped(typeof(ValidationFilter<>));
        services.AddValidatorsFromAssembly(
            typeof(ApplicationDbContext).Assembly,
            includeInternalTypes: true);
    }

    private static void AddApiVersioningConfiguration(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });
    }

    public static void AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Taskify API",
                Version = "v1"
            });

            options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Enter JWT token only (without 'Bearer ')"
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("bearer", document)] = []
            });
        });
    }
}