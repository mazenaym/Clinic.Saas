using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;

namespace Clinic.Saas.Infrastructure.Services;

public class PlatformPlanService : IPlanService
{
    private readonly IPlatformPlanRepository _plans;

    public PlatformPlanService(IPlatformPlanRepository plans)
    {
        _plans = plans;
    }

    public async Task<IReadOnlyList<PlatformPlanDto>> GetActivePlansAsync()
    {
        return await GetPlansAsync(false);
    }

    public async Task<IReadOnlyList<PlatformPlanDto>> GetPlansAsync(bool includeInactive = true)
    {
        var plans = await _plans.GetAllAsync(includeInactive);
        return plans.Select(Map).ToList();
    }

    public async Task<PlatformPlanDto?> GetPlanByIdAsync(Guid id)
    {
        var plan = await _plans.GetByIdAsync(id);
        return plan is null ? null : Map(plan);
    }

    public async Task<PlatformPlanDto?> GetPlanByCodeAsync(string code)
    {
        var plan = await _plans.GetByCodeAsync(NormalizeCode(code));
        return plan is null ? null : Map(plan);
    }

    public async Task<PlatformPlanDto> CreatePlanAsync(UpsertPlatformPlanDto dto)
    {
        var plan = BuildPlan(dto, Guid.NewGuid());
        return Map(await _plans.CreateAsync(plan));
    }

    public async Task<PlatformPlanDto?> UpdatePlanAsync(Guid id, UpsertPlatformPlanDto dto)
    {
        var existing = await _plans.GetByIdAsync(id);
        if (existing is null)
        {
            return null;
        }

        var plan = BuildPlan(dto, id);
        plan.CreatedAtUtc = existing.CreatedAtUtc;
        return (await _plans.UpdateAsync(plan)) is { } updated ? Map(updated) : null;
    }

    public Task<DeletePlanResult> DeletePlanAsync(Guid id)
    {
        return _plans.DeleteAsync(id);
    }

    public Task<bool> SetPlanActiveAsync(Guid id, bool isActive)
    {
        return _plans.UpdateStatusAsync(id, isActive);
    }

    private static SubscriptionPlan BuildPlan(UpsertPlatformPlanDto dto, Guid id) => new()
    {
        Id = id,
        Name = dto.Name.Trim(),
        Code = NormalizeCode(dto.Code),
        Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
        Price = dto.Price,
        Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "EGP" : dto.Currency.Trim().ToUpperInvariant(),
        DurationDays = dto.DurationDays,
        MaxUsers = dto.MaxUsers,
        MaxPatients = dto.MaxPatients,
        MaxDoctors = dto.MaxDoctors,
        FeaturesJson = string.IsNullOrWhiteSpace(dto.FeaturesJson) ? null : dto.FeaturesJson.Trim(),
        IsActive = dto.IsActive
    };

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    private static PlatformPlanDto Map(SubscriptionPlan plan) => new(
        plan.Id,
        plan.Name,
        plan.Code,
        plan.Description,
        plan.Price,
        plan.Currency,
        plan.DurationDays,
        plan.MaxUsers,
        plan.MaxPatients,
        plan.MaxDoctors,
        plan.FeaturesJson,
        plan.IsActive,
        plan.CreatedAtUtc,
        plan.UpdatedAtUtc);
}
