using System.Reflection;
using Clinic.Saas.api.Controllers;
using Clinic.Saas.Service.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Clinic.Saas.Tests;

public sealed class CanonicalRouteTests
{
    public static TheoryData<Type, string> CanonicalControllers => new()
    {
        { typeof(AuthController), "api/auth" },
        { typeof(UsersController), "api/users" },
        { typeof(TenantController), "api/tenant" },
        { typeof(PatientsController), "api/patients" },
        { typeof(PatientDocumentsController), "api/patient-documents" },
        { typeof(AppointmentsController), "api/appointments" },
        { typeof(OnlineBookingsController), "api/online-bookings" },
        { typeof(VisitsController), "api/visits" },
        { typeof(ClinicalTemplatesController), "api/clinical-templates" },
        { typeof(PrescriptionsController), "api/prescriptions" },
        { typeof(DrugCatalogController), "api/drug-catalog" },
        { typeof(ProceduresController), "api/procedures" },
        { typeof(InvoicesController), "api/invoices" },
        { typeof(BillingController), "api/billing" },
        { typeof(ReportsController), "api/reports" },
        { typeof(PlatformDashboardController), "api/platform/dashboard" },
        { typeof(PlatformClinicsController), "api/platform/clinics" },
        { typeof(PlatformPlansController), "api/platform/plans" },
        { typeof(PlatformSubscriptionsController), "api/platform/subscriptions" },
        { typeof(PlatformReportsController), "api/platform/reports" },
        { typeof(PlatformSettingsController), "api/platform/settings" },
        { typeof(PlatformAuditLogsController), "api/platform/audit-logs" }
    };

    [Theory]
    [MemberData(nameof(CanonicalControllers))]
    public void Controller_uses_explicit_lowercase_canonical_prefix(Type controller, string expected)
    {
        var route = controller.GetCustomAttribute<RouteAttribute>();

        Assert.NotNull(route);
        Assert.Equal(expected, route.Template);
        Assert.Equal(route.Template, route.Template!.ToLowerInvariant());
    }

    [Fact]
    public void Platform_controllers_remain_super_admin_only()
    {
        foreach (var controller in PlatformControllers()) Assert.Equal("SuperAdmin", Authorization(controller).Roles);
    }

    [Fact]
    public void Original_operations_god_controller_no_longer_exists()
    {
        Assert.Null(typeof(AuthController).Assembly.GetType("Clinic.Saas.api.Controllers.OperationsController"));
    }

    [Fact]
    public void Original_platform_god_controller_no_longer_exists() =>
        Assert.Null(typeof(AuthController).Assembly.GetType("Clinic.Saas.api.Controllers.PlatformController"));

    [Fact]
    public void Focused_platform_controllers_have_small_constructors()
    {
        foreach (var controller in PlatformControllers())
            Assert.True(controller.GetConstructors().Single().GetParameters().Length <= 6, controller.Name);
    }

    [Theory]
    [InlineData(typeof(PlatformDashboardController), "summary")]
    [InlineData(typeof(PlatformPlansController), "{id:guid}/status")]
    [InlineData(typeof(PlatformSubscriptionsController), "expiring-soon")]
    [InlineData(typeof(PlatformReportsController), "platform")]
    [InlineData(typeof(PlatformSettingsController), "platform")]
    public void Focused_controller_owns_expected_action_route(Type controller, string route)
    {
        Assert.Contains(controller.GetMethods(), method => method.GetCustomAttributes<HttpMethodAttribute>().Any(attribute => attribute.Template == route));
    }

    [Theory]
    [InlineData(typeof(AdminController))]
    [InlineData(typeof(AdminPlansController))]
    public void Legacy_admin_controllers_are_not_less_protected_than_platform(Type controller)
    {
        Assert.Equal("SuperAdmin", Authorization(controller).Roles);
        Assert.NotNull(controller.GetCustomAttribute<ObsoleteAttribute>());
        Assert.True(typeof(LegacyCompatibilityControllerBase).IsAssignableFrom(controller));
    }

    [Fact]
    public void Legacy_admin_controllers_depend_on_focused_facades_not_platform_handlers()
    {
        var controllers = new[] { typeof(AdminController), typeof(AdminPlansController), typeof(AdminReportsController) };
        foreach (var controller in controllers)
        {
            var dependencies = controller.GetConstructors().Single().GetParameters().Select(x => x.ParameterType).ToArray();
            Assert.DoesNotContain(dependencies, type => type.FullName?.Contains("UseCases.Admin") == true);
            Assert.All(dependencies.Where(type => type != typeof(Clinic.Saas.Service.Interfaces.ICurrentUserService)), type => Assert.Contains("Facade", type.Name));
        }
    }

    [Fact]
    public void User_listing_restores_admin_manage_policy()
    {
        var method = typeof(UsersController).GetMethod(nameof(UsersController.GetAll))!;
        var authorization = Authorization(method);

        Assert.Equal("Admin", authorization.Roles);
        Assert.Equal(Permissions.UsersManage, authorization.Policy);
    }

    [Theory]
    [InlineData(nameof(BillingController.MonthlyRevenue))]
    [InlineData(nameof(BillingController.DailyRevenue))]
    public void Revenue_reports_restore_admin_financial_policy(string methodName)
    {
        var authorization = Authorization(typeof(BillingController).GetMethod(methodName)!);

        Assert.Equal("Admin", authorization.Roles);
        Assert.Equal(Permissions.ReportsFinancialView, authorization.Policy);
    }

    private static AuthorizeAttribute Authorization(MemberInfo member) =>
        member.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Single();

    private static Type[] PlatformControllers() =>
    [
        typeof(PlatformDashboardController), typeof(PlatformClinicsController), typeof(PlatformPlansController),
        typeof(PlatformSubscriptionsController), typeof(PlatformReportsController), typeof(PlatformSettingsController),
        typeof(PlatformAuditLogsController)
    ];
}
