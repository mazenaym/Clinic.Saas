using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.Services;
using Clinic.Saas.Service.UseCases.Admin.Commands;
using Clinic.Saas.Service.UseCases.Admin.Queries;
using Clinic.Saas.Service.UseCases.Appointments.Commands;
using Clinic.Saas.Service.UseCases.Appointments.Queries;
using Clinic.Saas.Service.UseCases.Auth.Commands;
using Clinic.Saas.Service.UseCases.DrugCatalog.Queries;
using Clinic.Saas.Service.UseCases.Onboarding.Commands;
using Clinic.Saas.Service.UseCases.Onboarding.Queries;
using Clinic.Saas.Service.UseCases.PatientDocuments.Commands;
using Clinic.Saas.Service.UseCases.PatientDocuments.Queries;
using Clinic.Saas.Service.UseCases.Patients.Commands;
using Clinic.Saas.Service.UseCases.Patients.Queries;
using Clinic.Saas.Service.UseCases.Payments.Commands;
using Clinic.Saas.Service.UseCases.Payments.Queries;
using Clinic.Saas.Service.UseCases.Prescriptions.Commands;
using Clinic.Saas.Service.UseCases.Prescriptions.Queries;
using Clinic.Saas.Service.UseCases.Users.Commands;
using Clinic.Saas.Service.UseCases.Users.Queries;
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

        services.AddScoped<IClinicAuthorizationService, ClinicAuthorizationService>();

        services.AddScoped<GetAdminDashboardQuery.Handler>();
        services.AddScoped<GetAdminClinicsQuery.Handler>();
        services.AddScoped<GetAdminClinicByIdQuery.Handler>();
        services.AddScoped<BootstrapSuperAdminCommand.Handler>();
        services.AddScoped<CreateClinicCommand.Handler>();
        services.AddScoped<UpdateClinicCommand.Handler>();
        services.AddScoped<SetClinicStatusCommand.Handler>();
        services.AddScoped<CreateClinicSubscriptionCommand.Handler>();

        services.AddScoped<CreatePatientCommand.Handler>();
        services.AddScoped<GetPatientByIdQuery.Handler>();
        services.AddScoped<GetAllPatientsQuery.Handler>();
        services.AddScoped<SearchPatientsQuery.Handler>();
        services.AddScoped<GetPatientTimelineQuery.Handler>();
        services.AddScoped<FindPatientDuplicatesQuery.Handler>();
        services.AddScoped<ExportPatientsQuery.Handler>();
        services.AddScoped<UpdatePatientCommand.Handler>();
        services.AddScoped<DeletePatientCommand.Handler>();

        services.AddScoped<CreateAppointmentCommand.Handler>();
        services.AddScoped<GetAppointmentsByDateQuery.Handler>();
        services.AddScoped<GetAppointmentRangeQuery.Handler>();
        services.AddScoped<GetAppointmentCancellationsQuery.Handler>();
        services.AddScoped<GetAppointmentAvailabilityQuery.Handler>();
        services.AddScoped<RescheduleAppointmentCommand.Handler>();
        services.AddScoped<UpdateAppointmentStatusCommand.Handler>();

        services.AddScoped<CreateVisitCommand.Handler>();
        services.AddScoped<GetVisitByIdQuery.Handler>();
        services.AddScoped<GetPatientVisitsQuery.Handler>();
        services.AddScoped<UpdateVisitCommand.Handler>();
        services.AddScoped<FinalizeVisitCommand.Handler>();

        services.AddScoped<CreatePrescriptionCommand.Handler>();
        services.AddScoped<GetPrescriptionByIdQuery.Handler>();
        services.AddScoped<GetPrescriptionPdfQuery.Handler>();
        services.AddScoped<SendPrescriptionWhatsappCommand.Handler>();
        services.AddScoped<SearchDrugsQuery.Handler>();
        services.AddScoped<CheckDrugInteractionsQuery.Handler>();

        services.AddScoped<CreatePaymentCommand.Handler>();
        services.AddScoped<GetPaymentByIdQuery.Handler>();
        services.AddScoped<GetPatientPaymentsQuery.Handler>();
        services.AddScoped<UpdatePaymentCommand.Handler>();
        services.AddScoped<RefundPaymentCommand.Handler>();
        services.AddScoped<GetReceiptPdfQuery.Handler>();
        services.AddScoped<GetDebtTrackingQuery.Handler>();
        services.AddScoped<GetMonthlyRevenueQuery.Handler>();
        services.AddScoped<GetDailyRevenueReportQuery.Handler>();

        services.AddScoped<LoginCommand.Handler>();
        services.AddScoped<RefreshTokenCommand.Handler>();
        services.AddScoped<LogoutCommand.Handler>();
        services.AddScoped<ChangePasswordCommand.Handler>();

        services.AddScoped<RegisterClinicCommand.Handler>();
        services.AddScoped<CheckSubdomainAvailabilityQuery.Handler>();

        services.AddScoped<CreateUserCommand.Handler>();
        services.AddScoped<UpdateUserCommand.Handler>();
        services.AddScoped<DeactivateUserCommand.Handler>();
        services.AddScoped<ResetUserPasswordCommand.Handler>();
        services.AddScoped<GetTenantUsersQuery.Handler>();
        services.AddScoped<GetCurrentUserQuery.Handler>();
        services.AddScoped<GetUserPreferencesQuery.Handler>();
        services.AddScoped<SaveUserPreferencesCommand.Handler>();

        services.AddScoped<UploadPatientDocumentCommand.Handler>();
        services.AddScoped<GetPatientDocumentsQuery.Handler>();
        services.AddScoped<DownloadPatientDocumentQuery.Handler>();

        return services;
    }
}
