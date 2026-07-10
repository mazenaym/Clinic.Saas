using System.Reflection;
using Clinic.Saas.api.Controllers;
using Clinic.Saas.Service.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        { typeof(PlatformController), "api/platform" }
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
    public void Platform_controller_remains_super_admin_only()
    {
        var authorize = typeof(PlatformController)
            .GetCustomAttributes(inherit: true)
            .Single(attribute => attribute.GetType().Name == "AuthorizeAttribute");
        var roles = authorize.GetType().GetProperty("Roles")!.GetValue(authorize);

        Assert.Equal("SuperAdmin", roles);
    }

    [Fact]
    public void Original_operations_god_controller_no_longer_exists()
    {
        Assert.Null(typeof(AuthController).Assembly.GetType("Clinic.Saas.api.Controllers.OperationsController"));
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
}
