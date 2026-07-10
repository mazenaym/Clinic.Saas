using System.Reflection;
using Clinic.Saas.api.Controllers;
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
        { typeof(BillingController), "api/billing" },
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
}
