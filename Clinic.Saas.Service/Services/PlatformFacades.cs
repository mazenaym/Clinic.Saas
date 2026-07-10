using System.Data.Common;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.UseCases.Admin.Commands;
using Clinic.Saas.Service.UseCases.Admin.Queries;
using FluentValidation;

namespace Clinic.Saas.Service.Services;

public sealed class PlatformDashboardFacade(IPlatformDashboardService dashboard, GetAdminDashboardQuery.Handler legacy) : IPlatformDashboardFacade
{
    public Task<PlatformDashboardSummaryDto> GetSummaryAsync() => dashboard.GetDashboardSummaryAsync();
    public Task<BaseResponse<AdminDashboardStatsDto>> GetLegacySummaryAsync() => legacy.Handle();
}

public sealed class PlatformClinicsFacade(
    IPlatformDashboardService dashboard,
    GetAdminClinicsQuery.Handler list,
    GetAdminClinicByIdQuery.Handler get,
    CreateClinicCommand.Handler create,
    UpdateClinicCommand.Handler update,
    SetClinicStatusCommand.Handler status,
    CreateClinicSubscriptionCommand.Handler subscription,
    BootstrapSuperAdminCommand.Handler bootstrap) : IPlatformClinicsFacade
{
    public Task<IReadOnlyList<AdminClinicDto>> GetOverviewAsync(PlatformClinicFilterDto filter) => dashboard.GetClinicsOverviewAsync(filter);
    public Task<BaseResponse<IEnumerable<AdminClinicDto>>> GetLegacyListAsync() => list.Handle();
    public Task<BaseResponse<AdminClinicDto>> GetAsync(Guid id) => get.Handle(new GetAdminClinicByIdQuery.Query { ClinicId = id });
    public Task<BaseResponse<AdminClinicDto>> CreateAsync(CreateClinicDto dto) => create.Handle(new CreateClinicCommand.Command { Clinic = dto });
    public Task<BaseResponse<AdminClinicDto>> UpdateAsync(Guid id, UpdateClinicDto dto) => update.Handle(new UpdateClinicCommand.Command { ClinicId = id, Clinic = dto });
    public Task<BaseResponse<AdminClinicDto>> SetLegacyStatusAsync(Guid id, bool isActive) => status.Handle(new SetClinicStatusCommand.Command { ClinicId = id, IsActive = isActive });
    public Task<BaseResponse<Subscription>> CreateLegacySubscriptionAsync(Guid clinicId, CreateSubscriptionDto dto) => subscription.Handle(new CreateClinicSubscriptionCommand.Command { ClinicId = clinicId, Subscription = dto });
    public Task<BaseResponse<AdminClinicDto>> BootstrapAsync(BootstrapSuperAdminDto dto) => bootstrap.Handle(new BootstrapSuperAdminCommand.Command { Request = dto });
}

public sealed class PlatformPlansFacade(
    IPlanService plans,
    IValidator<CreatePlatformPlanRequest> createValidator,
    IValidator<UpdatePlatformPlanRequest> updateValidator,
    IValidator<UpdatePlatformPlanStatusRequest> statusValidator) : IPlatformPlansFacade
{
    public Task<IReadOnlyList<PlatformPlanDto>> GetAsync(bool includeInactive) => plans.GetPlansAsync(includeInactive);
    public Task<PlatformPlanDto?> GetAsync(Guid id) => plans.GetPlanByIdAsync(id);
    public Task<PlatformPlanDto> CreateAsync(UpsertPlatformPlanDto dto) => plans.CreatePlanAsync(dto);
    public Task<PlatformPlanDto?> UpdateAsync(Guid id, UpsertPlatformPlanDto dto) => plans.UpdatePlanAsync(id, dto);
    public Task<bool> DeleteAsync(Guid id) => plans.DeletePlanAsync(id);
    public Task<bool> SetActiveAsync(Guid id, bool active) => plans.SetPlanActiveAsync(id, active);

    public async Task<BaseResponse<PlatformPlanDto>> CreateLegacyAsync(CreatePlatformPlanRequest request)
    {
        var validation = await createValidator.ValidateAsync(request);
        if (!validation.IsValid) return Failure<PlatformPlanDto>("Validation failed for the request.", 422, validation.Errors.Select(x => x.ErrorMessage));
        var dto = Map(request);
        if (await plans.GetPlanByCodeAsync(dto.Code) is not null) return Failure<PlatformPlanDto>("Plan code already exists.", 409);
        return Success(await plans.CreatePlanAsync(dto), "Plan created.", 201);
    }

    public async Task<BaseResponse<PlatformPlanDto?>> UpdateLegacyAsync(Guid id, UpdatePlatformPlanRequest request)
    {
        var validation = await updateValidator.ValidateAsync(request);
        if (!validation.IsValid) return Failure<PlatformPlanDto?>("Validation failed for the request.", 422, validation.Errors.Select(x => x.ErrorMessage));
        var dto = Map(request);
        var duplicate = await plans.GetPlanByCodeAsync(dto.Code);
        if (duplicate is not null && duplicate.Id != id) return Failure<PlatformPlanDto?>("Plan code already exists.", 409);
        var updated = await plans.UpdatePlanAsync(id, dto);
        return updated is null ? Success<PlatformPlanDto?>(null, "Plan was not found.", 404) : Success<PlatformPlanDto?>(updated, "Plan updated.", 200);
    }

    public async Task<BaseResponse<bool>> DeleteLegacyAsync(Guid id)
    {
        try { return await plans.DeletePlanAsync(id) ? Success(true, "Plan deleted.", 200) : Success(false, "Plan was not found.", 404); }
        catch (DbException) { return Failure<bool>("Plan is linked to subscriptions and cannot be deleted.", 409); }
    }

    public async Task<BaseResponse<bool>> SetLegacyStatusAsync(Guid id, UpdatePlatformPlanStatusRequest request, string successMessage)
    {
        var validation = await statusValidator.ValidateAsync(request);
        if (!validation.IsValid) return Failure<bool>("Validation failed for the request.", 422, validation.Errors.Select(x => x.ErrorMessage));
        return await plans.SetPlanActiveAsync(id, request.IsActive) ? Success(true, successMessage, 200) : Success(false, "Plan was not found.", 404);
    }

    private static UpsertPlatformPlanDto Map(CreatePlatformPlanRequest x) => new(x.Name, x.Code, x.Description, x.Price, x.Currency, x.DurationDays, x.MaxUsers, x.MaxPatients, x.MaxDoctors, x.FeaturesJson, x.IsActive);
    private static UpsertPlatformPlanDto Map(UpdatePlatformPlanRequest x) => new(x.Name, x.Code, x.Description, x.Price, x.Currency, x.DurationDays, x.MaxUsers, x.MaxPatients, x.MaxDoctors, x.FeaturesJson, x.IsActive);
    private static BaseResponse<T> Success<T>(T data, string message, int code) => new() { Success = code < 400, Data = data, Message = message, StatusCode = code };
    private static BaseResponse<T> Failure<T>(string message, int code, IEnumerable<string>? errors = null) => new() { Success = false, Message = message, StatusCode = code, Errors = errors?.ToList() ?? [] };
}

public sealed class PlatformReportsFacade(
    IPlatformDashboardService dashboard,
    ISubscriptionService subscriptions,
    GetClinicUsageMetricsQuery.Handler usage,
    GetSubscriptionRevenueReportQuery.Handler revenue,
    GetExpiringSubscriptionsReportQuery.Handler expiring) : IPlatformReportsFacade
{
    public Task<PlatformDashboardSummaryDto> GetRevenueAsync() => dashboard.GetDashboardSummaryAsync();
    public Task<IReadOnlyList<TenantSubscriptionDto>> GetSubscriptionsAsync(PlatformSubscriptionFilterDto filter) => subscriptions.GetSubscriptionsAsync(filter);
    public Task<IReadOnlyList<AdminClinicDto>> GetClinicsAsync(PlatformClinicFilterDto filter) => dashboard.GetClinicsOverviewAsync(filter);
    public Task<PlatformReportsDto> GetPlatformAsync(PlatformReportsFilterDto filter) => dashboard.GetReportsAsync(filter);
    public Task<BaseResponse<List<ClinicUsageMetricDto>>> GetLegacyUsageAsync() => usage.Handle();
    public Task<BaseResponse<List<SubscriptionRevenueDto>>> GetLegacyRevenueAsync() => revenue.Handle();
    public Task<BaseResponse<List<ExpiringSubscriptionDto>>> GetLegacyExpiringAsync(int days) => expiring.Handle(new GetExpiringSubscriptionsReportQuery.Query { Days = days });
}

public sealed class PlatformAuditLogsFacade(GetActivityLogQuery.Handler handler) : IPlatformAuditLogsFacade
{
    public Task<BaseResponse<List<AuditLogDto>>> GetAsync(int take, bool includeAllTenants, Guid? tenantId) =>
        handler.Handle(new GetActivityLogQuery.Query { Take = take, IncludeAllTenants = includeAllTenants, TenantId = tenantId });
}
