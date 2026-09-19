using Asp.Versioning;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Taskify.WebApi.Infrastructure.Filters;
using Taskify.WebApi.Infrastructure.Middlewares;
using Taskify.WebApi.Persistence;
using Taskify.WebApi.Persistence.Interceptors;

namespace Taskify.WebApi.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();

        services.AddDbContext<ApplicationDbContext>((provider, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

            options.AddInterceptors(
                provider.GetRequiredService<SoftDeleteInterceptor>(),
                provider.GetRequiredService<AuditInterceptor>()
            );
        });
    }

    public static void AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddScoped(typeof(ValidationFilter<>));
        services.AddValidatorsFromAssembly(typeof(ApplicationDbContext).Assembly, includeInternalTypes: true);

        services.AddProblemDetails();
        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddApiVersioning();
    }

    private static void AddProblemDetails(this IServiceCollection services)
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
    }

    private static void AddApiVersioning(this IServiceCollection services)
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
}