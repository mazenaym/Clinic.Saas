using System.Text.Json;
using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.UseCases.Admin.Commands;
using Clinic.Saas.Service.UseCases.Admin.Queries;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Clinic.Saas.Service.Services;

public sealed class PlatformDashboardFacade(IPlatformDashboardService dashboard) : IPlatformDashboardFacade
{
    public Task<PlatformDashboardSummaryDto> GetSummaryAsync() => dashboard.GetDashboardSummaryAsync();
}

public sealed class PlatformClinicsFacade(
    IPlatformDashboardService dashboard,
    GetAdminClinicByIdQuery.Handler get,
    CreateClinicCommand.Handler create,
    UpdateClinicCommand.Handler update,
    SetClinicStatusCommand.Handler status,
    CreateClinicSubscriptionCommand.Handler legacySubscription,
    BootstrapSuperAdminCommand.Handler bootstrap,
    ISubscriptionService subscriptions,
    ILogger<PlatformClinicsFacade> logger) : IPlatformClinicsFacade
{
    public Task<IReadOnlyList<AdminClinicDto>> GetAsync(PlatformClinicFilterDto filter) => dashboard.GetClinicsOverviewAsync(filter);
    public Task<BaseResponse<AdminClinicDto>> GetByIdAsync(Guid id) => get.Handle(new GetAdminClinicByIdQuery.Query { ClinicId = id });
    public Task<BaseResponse<AdminClinicDto>> CreateAsync(CreateClinicDto dto) => create.Handle(new CreateClinicCommand.Command { Clinic = dto });

    public async Task<BaseResponse<AdminClinicDto>> CreateWithInitialSubscriptionAsync(CreateClinicDto dto, Guid? planId, Guid? actingUserId)
    {
        var result = await CreateAsync(dto);
        if (!result.Success || result.Data is null) return result;
        try
        {
            await subscriptions.CreateInitialSubscriptionAsync(result.Data.Id, planId, actingUserId);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Clinic {ClinicId} was created but its initial subscription failed", result.Data.Id);
            return new BaseResponse<AdminClinicDto> { Success = false, Message = "Clinic was created but the initial subscription could not be created.", Data = result.Data, StatusCode = 500 };
        }
    }

    public Task<BaseResponse<AdminClinicDto>> UpdateAsync(Guid id, UpdateClinicDto dto) => update.Handle(new UpdateClinicCommand.Command { ClinicId = id, Clinic = dto });
    public Task<BaseResponse<AdminClinicDto>> SetLegacyStatusAsync(Guid id, bool isActive) => status.Handle(new SetClinicStatusCommand.Command { ClinicId = id, IsActive = isActive });
    public Task<BaseResponse<AdminClinicDto>> CreateLegacySubscriptionAsync(Guid clinicId, CreateSubscriptionDto dto) => legacySubscription.Handle(new CreateClinicSubscriptionCommand.Command { ClinicId = clinicId, Subscription = dto });
    public Task<BaseResponse<AdminClinicDto>> BootstrapAsync(BootstrapSuperAdminDto dto) => bootstrap.Handle(new BootstrapSuperAdminCommand.Command { Request = dto });
}

public sealed class PlatformPlansFacade(IPlanService plans, IAuditService audit, ILogger<PlatformPlansFacade> logger) : IPlatformPlansFacade
{
    public Task<IReadOnlyList<PlatformPlanDto>> GetAsync(bool includeInactive) => plans.GetPlansAsync(includeInactive);
    public Task<PlatformPlanDto?> GetByIdAsync(Guid id) => plans.GetPlanByIdAsync(id);
    public Task<PlatformPlanDto?> GetByCodeAsync(string code) => plans.GetPlanByCodeAsync(code);

    public async Task<PlatformPlanDto> CreateAsync(UpsertPlatformPlanDto dto, Guid? actingUserId)
    {
        var result = await plans.CreatePlanAsync(dto);
        await AuditAsync("CreatePlan", result.Id, result, actingUserId);
        return result;
    }

    public async Task<PlatformPlanDto?> UpdateAsync(Guid id, UpsertPlatformPlanDto dto, Guid? actingUserId)
    {
        var result = await plans.UpdatePlanAsync(id, dto);
        if (result is not null) await AuditAsync("UpdatePlan", id, result, actingUserId);
        return result;
    }

    public async Task<DeletePlanResult> DeleteAsync(Guid id, Guid? actingUserId)
    {
        var result = await plans.DeletePlanAsync(id);
        if (result == DeletePlanResult.Deleted) await AuditAsync("DeletePlan", id, new { id }, actingUserId);
        return result;
    }

    public async Task<bool> SetActiveAsync(Guid id, bool active, Guid? actingUserId)
    {
        var result = await plans.SetPlanActiveAsync(id, active);
        if (result) await AuditAsync(active ? "ActivatePlan" : "DeactivatePlan", id, new { id, IsActive = active }, actingUserId);
        return result;
    }

    private async Task AuditAsync(string action, Guid id, object values, Guid? userId)
    {
        try { await audit.LogAsync(new AuditEntry { UserId = userId, Action = action, EntityName = "SubscriptionPlan", EntityId = id, NewValues = JsonSerializer.Serialize(values), CreatedAt = DateTime.UtcNow }); }
        catch (Exception ex) { logger.LogError(ex, "Audit failed for {Action} on plan {PlanId}", action, id); }
    }
}

public sealed class PlatformReportsFacade(IPlatformDashboardService dashboard, ISubscriptionService subscriptions) : IPlatformReportsFacade
{
    public Task<PlatformDashboardSummaryDto> GetRevenueAsync() => dashboard.GetDashboardSummaryAsync();
    public Task<IReadOnlyList<TenantSubscriptionDto>> GetSubscriptionsAsync(PlatformSubscriptionFilterDto filter) => subscriptions.GetSubscriptionsAsync(filter);
    public Task<IReadOnlyList<AdminClinicDto>> GetClinicsAsync(PlatformClinicFilterDto filter) => dashboard.GetClinicsOverviewAsync(filter);
    public Task<PlatformReportsDto> GetPlatformAsync(PlatformReportsFilterDto filter) => dashboard.GetReportsAsync(filter);
}

public sealed class PlatformSubscriptionsFacade(ISubscriptionService subscriptions, IValidator<RenewTenantSubscriptionRequest> validator) : IPlatformSubscriptionsFacade
{
    public Task<TenantSubscriptionDto?> GetByIdAsync(Guid id) => subscriptions.GetSubscriptionByIdAsync(id);
    public Task<IReadOnlyList<TenantSubscriptionDto>> GetExpiringAsync(int days) => subscriptions.GetExpiringSoonSubscriptionsAsync(days);
    public Task<BaseResponse<TenantSubscriptionDto>> RenewAsync(Guid tenantId, RenewTenantSubscriptionRequest request, Guid? actingUserId) => ExecuteAsync(tenantId, request, actingUserId, false);
    public Task<BaseResponse<TenantSubscriptionDto>> ChangePlanAsync(Guid tenantId, RenewTenantSubscriptionRequest request, Guid? actingUserId) => ExecuteAsync(tenantId, request, actingUserId, true);

    private async Task<BaseResponse<TenantSubscriptionDto>> ExecuteAsync(Guid tenantId, RenewTenantSubscriptionRequest request, Guid? userId, bool changePlan)
    {
        var normalized = request with { TenantId = tenantId };
        var validation = await validator.ValidateAsync(normalized);
        if (!validation.IsValid) return new BaseResponse<TenantSubscriptionDto> { Success = false, Message = changePlan ? "Subscription change-plan request is invalid." : "Subscription renewal request is invalid.", Errors = validation.Errors.Select(x => x.ErrorMessage).ToList(), StatusCode = 400 };
        var result = changePlan ? await subscriptions.ChangeSubscriptionPlanAsync(normalized, userId) : await subscriptions.RenewSubscriptionAsync(normalized, userId);
        return result is null
            ? new BaseResponse<TenantSubscriptionDto> { Success = false, Message = "Clinic was not found.", StatusCode = 404 }
            : new BaseResponse<TenantSubscriptionDto> { Success = true, Data = result, Message = changePlan ? "Subscription plan changed." : "Subscription renewed.", StatusCode = 200 };
    }
}

public sealed class PlatformAuditLogsFacade(GetActivityLogQuery.Handler handler) : IPlatformAuditLogsFacade
{
    public Task<BaseResponse<List<AuditLogDto>>> GetAsync(int take, bool includeAllTenants, Guid? tenantId) => handler.Handle(new GetActivityLogQuery.Query { Take = take, IncludeAllTenants = includeAllTenants, TenantId = tenantId });
}
