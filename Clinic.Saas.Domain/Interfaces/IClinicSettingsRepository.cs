namespace Clinic.Saas.Domain.Interfaces;

public interface IClinicSettingsRepository
{
    Task<bool> IsWhatsappEnabledAsync(Guid tenantId);
}
