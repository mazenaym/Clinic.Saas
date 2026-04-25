using Clinic.Saas.Service.UseCases.Appointments.Commands;
using Clinic.Saas.Service.UseCases.Appointments.Queries;
using Clinic.Saas.Service.UseCases.Patients.Commands;
using Clinic.Saas.Service.UseCases.Patients.Queries;
using Clinic.Saas.Service.UseCases.Payments.Commands;
using Clinic.Saas.Service.UseCases.Payments.Queries;
using Clinic.Saas.Service.UseCases.Prescriptions.Commands;
using Clinic.Saas.Service.UseCases.Prescriptions.Queries;
using Clinic.Saas.Service.UseCases.Visits.Commands;
using Clinic.Saas.Service.UseCases.Visits.Queries;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Clinic.Saas.Service;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => { }, typeof(DependencyInjection).Assembly);
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<CreatePatientCommand.Handler>();
        services.AddScoped<GetPatientByIdQuery.Handler>();
        services.AddScoped<GetAllPatientsQuery.Handler>();
        services.AddScoped<SearchPatientsQuery.Handler>();
        services.AddScoped<UpdatePatientCommand.Handler>();
        services.AddScoped<DeletePatientCommand.Handler>();

        services.AddScoped<CreateAppointmentCommand.Handler>();
        services.AddScoped<GetAppointmentsByDateQuery.Handler>();
        services.AddScoped<GetAppointmentAvailabilityQuery.Handler>();
        services.AddScoped<UpdateAppointmentStatusCommand.Handler>();

        services.AddScoped<CreateVisitCommand.Handler>();
        services.AddScoped<GetVisitByIdQuery.Handler>();

        services.AddScoped<CreatePrescriptionCommand.Handler>();
        services.AddScoped<GetPrescriptionByIdQuery.Handler>();

        services.AddScoped<CreatePaymentCommand.Handler>();
        services.AddScoped<GetDailyRevenueReportQuery.Handler>();

        return services;
    }
}
