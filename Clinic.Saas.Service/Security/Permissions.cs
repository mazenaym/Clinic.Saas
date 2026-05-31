using Clinic.Saas.Domain.Enums;

namespace Clinic.Saas.Service.Security;

public static class Permissions
{
    public const string PatientsView = "patients.view";
    public const string PatientsEdit = "patients.edit";
    public const string PatientsClinicalView = "patients.clinical.view";
    public const string AppointmentsManage = "appointments.manage";
    public const string VisitsManage = "visits.manage";
    public const string PrescriptionsManage = "prescriptions.manage";
    public const string BillingView = "billing.view";
    public const string BillingManage = "billing.manage";
    public const string ReportsFinancialView = "reports.financial.view";
    public const string SettingsManage = "settings.manage";
    public const string UsersManage = "users.manage";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        PatientsView,
        PatientsEdit,
        PatientsClinicalView,
        AppointmentsManage,
        VisitsManage,
        PrescriptionsManage,
        BillingView,
        BillingManage,
        ReportsFinancialView,
        SettingsManage,
        UsersManage
    };

    public static readonly IReadOnlyDictionary<UserRole, IReadOnlySet<string>> RolePermissions =
        new Dictionary<UserRole, IReadOnlySet<string>>
        {
            [UserRole.SuperAdmin] = All,
            [UserRole.Admin] = All,
            [UserRole.Doctor] = new HashSet<string>
            {
                PatientsView,
                PatientsEdit,
                PatientsClinicalView,
                AppointmentsManage,
                VisitsManage,
                PrescriptionsManage,
                BillingView
            },
            [UserRole.Reception] = new HashSet<string>
            {
                PatientsView,
                PatientsEdit,
                AppointmentsManage,
                BillingView,
                BillingManage,
                ReportsFinancialView
            }
        };
}
