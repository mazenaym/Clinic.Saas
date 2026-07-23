using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Infrastructure.Data;
using Clinic.Saas.Infrastructure.Repositories;
using Clinic.Saas.Infrastructure.Services;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Clinic.Saas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IHostEnvironment env)
    {
        services.AddSingleton<DapperContext>();
        services.AddScoped<IDbConnectionFactory, DapperConnectionFactory>();
        services.AddHttpContextAccessor();

        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IOnlineBookingRepository, OnlineBookingRepository>();
        services.AddScoped<IClinicalTemplateRepository, ClinicalTemplateRepository>();
        services.AddScoped<IVisitRepository, VisitRepository>();
        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
        services.AddScoped<IDrugCatalogRepository, DrugCatalogRepository>();
        services.AddScoped<IProcedureRepository, ProcedureRepository>();
        services.AddScoped<IClinicSettingsRepository, ClinicSettingsRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IPlatformAdminRepository, PlatformAdminRepository>();
        services.AddScoped<IPlatformPlanRepository, PlatformPlanRepository>();
        services.AddScoped<IAdminReportRepository, AdminReportRepository>();
        services.AddScoped<IOperationsTenantRepository, OperationsTenantRepository>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IPlanService, PlatformPlanService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IPlatformDashboardService, PlatformDashboardService>();

        if (env.IsDevelopment())
        {
            services.AddScoped<IEmailSender, NoopEmailSender>();
            services.AddScoped<IWhatsAppSender, NoopWhatsAppSender>();
        }

        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddScoped<IPatientDocumentRepository, PatientDocumentRepository>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();



        return services;
    }
}
