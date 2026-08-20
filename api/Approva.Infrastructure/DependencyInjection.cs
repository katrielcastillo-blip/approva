using Approva.Application.Common.Interfaces;
using Approva.Infrastructure.Auth;
using Approva.Infrastructure.BackgroundJobs;
using Approva.Infrastructure.Notifications;
using Approva.Infrastructure.Persistence;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Approva.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");

        services.AddDbContext<ApprovaDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IApprovaDbContext>(sp => sp.GetRequiredService<ApprovaDbContext>());

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        var resendApiKey = configuration["RESEND_API_KEY"] ?? Environment.GetEnvironmentVariable("RESEND_API_KEY");
        if (string.IsNullOrWhiteSpace(resendApiKey))
        {
            services.AddSingleton<INotificationSender, LogNotificationSender>();
        }
        else
        {
            var fromAddress = configuration["Resend:FromAddress"] ?? "Approva <notifications@approva.dev>";
            services.AddHttpClient<ResendNotificationSender>();
            services.AddSingleton<INotificationSender>(sp =>
            {
                var httpClient = sp.GetRequiredService<System.Net.Http.IHttpClientFactory>().CreateClient(nameof(ResendNotificationSender));
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ResendNotificationSender>>();
                return new ResendNotificationSender(httpClient, resendApiKey, fromAddress, logger);
            });
        }

        services.AddScoped<SlaEscalationJob>();

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString)));
        services.AddHangfireServer();

        return services;
    }
}
