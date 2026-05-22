namespace Clinic.Saas.Service.DTOs;

public class AdminDashboardStatsDto
{
    public int TotalClinics { get; set; }
    public int ActiveClinics { get; set; }
    public int InactiveClinics { get; set; }
    public int TotalUsers { get; set; }
    public int TotalPatients { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int TrialSubscriptions { get; set; }
    public int ExpiredSubscriptions { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal TodayRevenue { get; set; }
    public List<AdminClinicDto> RecentClinics { get; set; } = new();
}
