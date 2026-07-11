using Clinic.Saas.Domain.Enums;

namespace Clinic.Saas.Service.DTOs;

public record PlatformPlanDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    decimal Price,
    string Currency,
    int DurationDays,
    int? MaxUsers,
    int? MaxPatients,
    int? MaxDoctors,
    string? FeaturesJson,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public record UpsertPlatformPlanDto(
    string Name,
    string Code,
    string? Description,
    decimal Price,
    string? Currency,
    int DurationDays,
    int? MaxUsers,
    int? MaxPatients,
    int? MaxDoctors,
    string? FeaturesJson,
    bool IsActive = true);

public record CreatePlatformPlanRequest(
    string Name,
    string Code,
    string? Description,
    decimal Price,
    string? Currency,
    int DurationDays,
    int? MaxUsers,
    int? MaxPatients,
    int? MaxDoctors,
    string? FeaturesJson,
    bool IsActive = true);

public record UpdatePlatformPlanRequest(
    string Name,
    string Code,
    string? Description,
    decimal Price,
    string? Currency,
    int DurationDays,
    int? MaxUsers,
    int? MaxPatients,
    int? MaxDoctors,
    string? FeaturesJson,
    bool IsActive = true);

public record UpdatePlatformPlanStatusRequest(bool IsActive);

public record TenantSubscriptionDto(
    Guid Id,
    Guid TenantId,
    string TenantName,
    Guid PlanId,
    string PlanName,
    string PlanCode,
    SubscriptionStatus Status,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    DateTime? RenewedAtUtc,
    DateTime? CancelledAtUtc,
    DateTime? SuspendedAtUtc,
    bool AutoRenew,
    int GracePeriodDays,
    DateTime? LastCheckedAtUtc,
    string? Notes,
    int DaysRemaining,
    bool IsInGracePeriod,
    decimal? ActualPaidAmount = null,
    DateTime? PaymentDateUtc = null,
    string? PaymentMethod = null);

public record SubscriptionStatusDto(
    Guid TenantId,
    SubscriptionStatus? SubscriptionStatus,
    DateTime? SubscriptionEndsAtUtc,
    bool IsInGracePeriod,
    int DaysRemaining,
    string TenantStatus,
    bool IsActive);

public record RenewTenantSubscriptionRequest(
    Guid TenantId,
    Guid PlanId,
    DateTime? CustomEndDateUtc,
    decimal? ActualPaidAmount,
    DateTime? PaymentDateUtc,
    string? PaymentMethod,
    string? Notes);

public record PlatformRevenueReportDto(
    decimal CurrentMonthRevenue,
    decimal CurrentYearRevenue,
    decimal TotalCollected,
    int PaymentCount,
    decimal AveragePayment);

public record PlatformSubscriptionStatusReportDto(
    int Active,
    int Trial,
    int Expired,
    int Suspended,
    int ExpiringSoon);

public record PlatformSubscriptionPaymentDto(
    Guid Id,
    Guid TenantId,
    string ClinicName,
    Guid? SubscriptionId,
    Guid? PlanId,
    string PlanName,
    decimal Amount,
    string Currency,
    DateTime PaymentDateUtc,
    string? PaymentMethod,
    string? Notes);

public record PlatformReportsDto(
    PlatformRevenueReportDto Revenue,
    PlatformSubscriptionStatusReportDto Subscriptions,
    IReadOnlyList<PlatformSubscriptionPaymentDto> RecentPayments);

public record PlatformReportsFilterDto(
    DateTime? From,
    DateTime? To,
    Guid? TenantId,
    Guid? PlanId);

public sealed record PlatformRevenueAnalyticsDto(
    decimal CurrentMonthRevenue,
    decimal PreviousMonthRevenue,
    decimal CurrentMonthChangePercentage,
    decimal CurrentYearRevenue,
    decimal PreviousYearRevenue,
    decimal CurrentYearChangePercentage,
    DateTime FromUtc,
    DateTime ToUtc,
    IReadOnlyList<PlatformWeeklyRevenueDto> WeeklyRevenue,
    IReadOnlyList<PlatformMonthlyRevenueDto> MonthlyRevenue);

public sealed record PlatformWeeklyRevenueDto(DateTime WeekStartUtc, DateTime WeekEndUtc, int Year, int WeekNumber, string Label, decimal Revenue, int PaymentsCount, int ClinicsCount);
public sealed record PlatformMonthlyRevenueDto(int Year, int Month, string MonthKey, string MonthLabel, decimal Revenue, int PaymentsCount, int ClinicsCount);

public sealed record PlatformRevenueAnalyticsFilterDto(DateTime? From, DateTime? To, int? Year, Guid? TenantId, Guid? PlanId);

public record PlatformSettingsDto(
    int TrialDurationDays,
    int ExpiringSoonThresholdDays,
    int DefaultGracePeriodDays,
    bool AutoSuspendExpiredClinics,
    string CurrencyCode,
    string? PlatformSupportEmail,
    string? PlatformSupportPhone,
    string? PaymentMethodsEnabled,
    decimal? TaxPercentage);

public record SuspendTenantDto(string Reason);

public record PlatformClinicFilterDto(
    string? Search,
    string? Status,
    Guid? PlanId,
    SubscriptionStatus? SubscriptionStatus,
    DateTime? ExpiresBefore,
    DateTime? ExpiresAfter,
    int Page = 1,
    int PageSize = 50);

public record PlatformSubscriptionFilterDto(
    SubscriptionStatus? Status,
    Guid? PlanId,
    Guid? TenantId,
    DateTime? ExpiresBefore,
    DateTime? ExpiresAfter,
    int Page = 1,
    int PageSize = 50);

public record PlatformDashboardSummaryDto(
    int TotalClinics,
    int ActiveClinics,
    int TrialClinics,
    int ExpiredClinics,
    int SuspendedClinics,
    int TotalUsers,
    int TotalPatients,
    int TotalAppointments,
    decimal MonthlySubscriptionRevenue,
    decimal AnnualSubscriptionRevenue,
    int ExpiringSoonCount,
    int ExpiredSubscriptionsCount,
    IReadOnlyList<AdminClinicDto> RecentClinics,
    IReadOnlyList<TenantSubscriptionDto> SubscriptionAlerts);

public record SubscriptionExpiryResultDto(
    int Checked,
    int MarkedPastDue,
    int MarkedExpired,
    int Skipped,
    int Errors);
