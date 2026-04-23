using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Infrastructure.Data;
using Clinic.Saas.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Clinic.Saas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services)
    {
        // Dapper Context
        services.AddSingleton<DapperContext>();

        // Repositories
        services.AddScoped<IPatientRepository, PatientRepository>();
        // services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        // ... باقي الـ Repositories

        return services;
    }
}