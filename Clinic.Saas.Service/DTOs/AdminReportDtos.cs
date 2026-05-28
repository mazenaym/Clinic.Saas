namespace Clinic.Saas.Service.DTOs;

public class ClinicUsageMetricDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public int UsersCount { get; set; }
    public int PatientsCount { get; set; }
    public int AppointmentsCount { get; set; }
}

public class SubscriptionRevenueDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Revenue { get; set; }
    public int SubscriptionCount { get; set; }
}

public class ExpiringSubscriptionDto
{
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
