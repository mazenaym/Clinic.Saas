using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.Interfaces;

public interface IAuditService
{
    Task LogAsync(AuditEntry entry);
}
