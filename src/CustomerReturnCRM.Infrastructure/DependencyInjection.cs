using CustomerReturnCRM.Application.BusinessSetup;
using CustomerReturnCRM.Application.CustomerManagement;
using CustomerReturnCRM.Application.AppointmentManagement;
using CustomerReturnCRM.Application.ServiceManagement;
using CustomerReturnCRM.Application.VisitManagement;
using CustomerReturnCRM.Application.Authentication;
using CustomerReturnCRM.Application.ReturnAnalysis;
using CustomerReturnCRM.Application.ReminderManagement;
using CustomerReturnCRM.Application.Dashboard;
using CustomerReturnCRM.Application.ServiceTemplateManagement;
using CustomerReturnCRM.Application.StaffManagement;
using CustomerReturnCRM.Application.Sms;
using CustomerReturnCRM.Infrastructure.CustomerManagement;
using CustomerReturnCRM.Infrastructure.CustomerProfile;
using CustomerReturnCRM.Infrastructure.AppointmentManagement;
using CustomerReturnCRM.Infrastructure.BusinessSetup;
using CustomerReturnCRM.Infrastructure.Authentication;
using CustomerReturnCRM.Infrastructure.Identity;
using CustomerReturnCRM.Infrastructure.Persistence;
using CustomerReturnCRM.Infrastructure.ReturnAnalysis;
using CustomerReturnCRM.Infrastructure.ReminderManagement;
using CustomerReturnCRM.Infrastructure.Dashboard;
using CustomerReturnCRM.Infrastructure.ServiceTemplateManagement;
using CustomerReturnCRM.Infrastructure.StaffManagement;
using CustomerReturnCRM.Infrastructure.ServiceManagement;
using CustomerReturnCRM.Infrastructure.VisitManagement;
using CustomerReturnCRM.Infrastructure.Sms;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerReturnCRM.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
        services.AddScoped<IBusinessSetupService, BusinessSetupService>();
        services.AddScoped<ICustomerManagementService, CustomerManagementService>();
        services.AddScoped<ICustomerProfileService, CustomerProfileService>();
        services.AddScoped<IAppointmentManagementService, AppointmentManagementService>();
        services.AddScoped<IServiceManagementService, ServiceManagementService>();
        services.AddScoped<IVisitManagementService, VisitManagementService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IReturnAnalysisService, ReturnAnalysisService>();
        services.AddScoped<IReminderManagementService, ReminderManagementService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IServiceTemplateManagementService, ServiceTemplateManagementService>();
        services.AddScoped<IStaffManagementService, StaffManagementService>();
        services.AddScoped<ISmsManagementService, SmsManagementService>();
        services.AddSingleton<ISmsProvider, LoggingSmsProvider>();
        services.AddHostedService<SmsSendingBackgroundService>();
        services.AddSingleton(TimeProvider.System);
        services.Configure<ReturnAnalysisOptions>(options =>
        {
            options.DueSoonDays = GetNonNegativeSetting(configuration, nameof(ReturnAnalysisOptions.DueSoonDays), options.DueSoonDays);
            options.AtRiskDays = GetNonNegativeSetting(configuration, nameof(ReturnAnalysisOptions.AtRiskDays), options.AtRiskDays);
            options.NoRecentVisitDays = GetNonNegativeSetting(configuration, nameof(ReturnAnalysisOptions.NoRecentVisitDays), options.NoRecentVisitDays);
        });

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        return services;
    }

    private static int GetNonNegativeSetting(IConfiguration configuration, string settingName, int defaultValue)
    {
        var key = $"{ReturnAnalysisOptions.SectionName}:{settingName}";
        var configuredValue = configuration[key];
        if (string.IsNullOrWhiteSpace(configuredValue)) return defaultValue;
        if (!int.TryParse(configuredValue, out var parsedValue) || parsedValue < 0)
            throw new InvalidOperationException($"Configuration value '{key}' must be a non-negative integer.");
        return parsedValue;
    }
}
