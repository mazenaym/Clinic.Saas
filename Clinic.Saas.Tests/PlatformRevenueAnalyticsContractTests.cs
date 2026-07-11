using Clinic.Saas.api.Controllers;
using Clinic.Saas.Service.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.Tests;

public sealed class PlatformRevenueAnalyticsContractTests
{
    [Fact]
    public void RevenueController_IsRestrictedToSuperAdmin()
    {
        var attribute = Assert.Single(typeof(PlatformRevenueController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal("SuperAdmin", attribute.Roles);
    }

    [Fact]
    public void AnalyticsAction_UsesTypedQueryContract()
    {
        var action = typeof(PlatformRevenueController).GetMethod(nameof(PlatformRevenueController.Analytics));
        Assert.NotNull(action);
        Assert.Equal(typeof(PlatformRevenueAnalyticsFilterDto), Assert.Single(action!.GetParameters()).ParameterType);
        Assert.NotNull(action.GetParameters()[0].GetCustomAttributes(typeof(FromQueryAttribute), true).SingleOrDefault());
    }

    [Fact]
    public void ZeroValuePeriods_AreRepresentableWithoutNulls()
    {
        var month = new PlatformMonthlyRevenueDto(2026, 2, "2026-02", "Feb 2026", 0, 0, 0);
        var week = new PlatformWeeklyRevenueDto(new DateTime(2026, 2, 2), new DateTime(2026, 2, 8), 2026, 6, "W06 2026", 0, 0, 0);
        Assert.Equal(0, month.Revenue); Assert.Equal(DayOfWeek.Monday, week.WeekStartUtc.DayOfWeek);
    }
}
