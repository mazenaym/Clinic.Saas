using Clinic.Saas.Service.UseCases.Patients.Commands;
using Clinic.Saas.Service.UseCases.Patients.Queries;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Clinic.Saas.Service;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        // AutoMapper
        services.AddAutoMapper(cfg => { }, typeof(DependencyInjection).Assembly);

        // FluentValidation
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Use Cases
        services.AddScoped<CreatePatientCommand.Handler>();
        services.AddScoped<GetPatientByIdQuery.Handler>();

        return services;
    }
}
