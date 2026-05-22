using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Infrastructure.Data;
using Clinic.Saas.Infrastructure.Repositories;
using Clinic.Saas.Infrastructure.Services;
using Clinic.Saas.Service.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Clinic.Saas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<DapperContext>();
        services.AddHttpContextAccessor();

        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IVisitRepository, VisitRepository>();
        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IPlatformAdminRepository, PlatformAdminRepository>();

        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }
}
