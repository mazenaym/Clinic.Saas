using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.Services;
using Clinic.Saas.Service.Validators;
using Microsoft.Extensions.Logging.Abstractions;

namespace Clinic.Saas.Tests;

public sealed class PlatformFacadeTests
{
    [Theory]
    [InlineData(DeletePlanResult.Deleted, 1)]
    [InlineData(DeletePlanResult.NotFound, 0)]
    [InlineData(DeletePlanResult.InUse, 0)]
    public async Task Plan_delete_returns_typed_outcome_and_audits_only_success(DeletePlanResult outcome, int audits)
    {
        var plans = new FakePlans { DeleteResult = outcome };
        var audit = new RecordingAudit();
        var facade = new PlatformPlansFacade(plans, audit, NullLogger<PlatformPlansFacade>.Instance);
        Assert.Equal(outcome, await facade.DeleteAsync(Guid.NewGuid(), Guid.NewGuid()));
        Assert.Equal(audits, audit.Entries.Count);
    }

    [Theory]
    [InlineData(true, "ActivatePlan")]
    [InlineData(false, "DeactivatePlan")]
    public async Task Every_plan_status_path_audits_exactly_once(bool active, string action)
    {
        var audit = new RecordingAudit();
        var facade = new PlatformPlansFacade(new FakePlans(), audit, NullLogger<PlatformPlansFacade>.Instance);
        Assert.True(await facade.SetActiveAsync(Guid.NewGuid(), active, Guid.NewGuid()));
        Assert.Single(audit.Entries);
        Assert.Equal(action, audit.Entries[0].Action);
    }

    [Fact]
    public async Task Change_plan_uses_distinct_service_operation_and_message()
    {
        var service = new FakeSubscriptions();
        var facade = new PlatformSubscriptionsFacade(service, new RenewTenantSubscriptionRequestValidator());
        var result = await facade.ChangePlanAsync(Guid.NewGuid(), Request(), Guid.NewGuid());
        Assert.True(result.Success);
        Assert.Equal("Subscription plan changed.", result.Message);
        Assert.Equal(1, service.ChangeCalls);
        Assert.Equal(0, service.RenewCalls);
    }

    [Fact]
    public async Task Direct_lookup_does_not_use_paginated_listing()
    {
        var service = new FakeSubscriptions();
        var facade = new PlatformSubscriptionsFacade(service, new RenewTenantSubscriptionRequestValidator());
        await facade.GetByIdAsync(Guid.NewGuid());
        Assert.Equal(1, service.LookupCalls);
        Assert.Equal(0, service.ListCalls);
    }

    private static RenewTenantSubscriptionRequest Request() => new(Guid.NewGuid(), Guid.NewGuid(), null, null, null, null, null);

    private sealed class RecordingAudit : IAuditService
    {
        public List<AuditEntry> Entries { get; } = [];
        public Task LogAsync(AuditEntry entry) { Entries.Add(entry); return Task.CompletedTask; }
    }

    private sealed class FakePlans : IPlanService
    {
        public DeletePlanResult DeleteResult { get; set; } = DeletePlanResult.Deleted;
        public Task<DeletePlanResult> DeletePlanAsync(Guid id) => Task.FromResult(DeleteResult);
        public Task<bool> SetPlanActiveAsync(Guid id, bool isActive) => Task.FromResult(true);
        public Task<IReadOnlyList<PlatformPlanDto>> GetActivePlansAsync() => throw new NotSupportedException();
        public Task<IReadOnlyList<PlatformPlanDto>> GetPlansAsync(bool includeInactive = true) => throw new NotSupportedException();
        public Task<PlatformPlanDto?> GetPlanByIdAsync(Guid id) => throw new NotSupportedException();
        public Task<PlatformPlanDto?> GetPlanByCodeAsync(string code) => throw new NotSupportedException();
        public Task<PlatformPlanDto> CreatePlanAsync(UpsertPlatformPlanDto dto) => throw new NotSupportedException();
        public Task<PlatformPlanDto?> UpdatePlanAsync(Guid id, UpsertPlatformPlanDto dto) => throw new NotSupportedException();
    }

    private sealed class FakeSubscriptions : ISubscriptionService
    {
        public int ChangeCalls, RenewCalls, LookupCalls, ListCalls;
        public Task<TenantSubscriptionDto?> GetSubscriptionByIdAsync(Guid id) { LookupCalls++; return Task.FromResult<TenantSubscriptionDto?>(null); }
        public Task<IReadOnlyList<TenantSubscriptionDto>> GetSubscriptionsAsync(PlatformSubscriptionFilterDto filter) { ListCalls++; return Task.FromResult<IReadOnlyList<TenantSubscriptionDto>>([]); }
        public Task<TenantSubscriptionDto?> RenewSubscriptionAsync(RenewTenantSubscriptionRequest request, Guid? user) { RenewCalls++; return Task.FromResult<TenantSubscriptionDto?>(Subscription(request)); }
        public Task<TenantSubscriptionDto?> ChangeSubscriptionPlanAsync(RenewTenantSubscriptionRequest request, Guid? user) { ChangeCalls++; return Task.FromResult<TenantSubscriptionDto?>(Subscription(request)); }
        private static TenantSubscriptionDto Subscription(RenewTenantSubscriptionRequest r) => new(Guid.NewGuid(), r.TenantId, "Clinic", r.PlanId, "Plan", "PLAN", SubscriptionStatus.Active, DateTime.UtcNow, DateTime.UtcNow.AddDays(1), null, null, null, false, 0, null, null, 1, false);
        public Task<TenantSubscriptionDto> CreateInitialSubscriptionAsync(Guid tenantId, Guid? planId, Guid? user = null) => throw new NotSupportedException();
        public Task<TenantSubscriptionDto?> GetCurrentSubscriptionAsync(Guid tenantId) => throw new NotSupportedException();
        public Task<SubscriptionStatusDto> GetSubscriptionStatusAsync(Guid tenantId) => throw new NotSupportedException();
        public Task<bool> SuspendTenantAsync(Guid tenantId, string reason, Guid? user) => throw new NotSupportedException();
        public Task<bool> DisableTenantAsync(Guid tenantId, string reason, Guid? user) => throw new NotSupportedException();
        public Task<bool> ReactivateTenantAsync(Guid tenantId, Guid? user) => throw new NotSupportedException();
        public Task<bool> CancelSubscriptionAsync(Guid id, string? reason, Guid? user) => throw new NotSupportedException();
        public Task<SubscriptionExpiryResultDto> CheckAndExpireSubscriptionsAsync() => throw new NotSupportedException();
        public Task<IReadOnlyList<TenantSubscriptionDto>> GetExpiringSoonSubscriptionsAsync(int days) => throw new NotSupportedException();
        public Task<IReadOnlyList<TenantSubscriptionDto>> GetExpiredSubscriptionsAsync() => throw new NotSupportedException();
    }
}
