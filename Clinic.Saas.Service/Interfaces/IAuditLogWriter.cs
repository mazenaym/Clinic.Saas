namespace Clinic.Saas.Service.Interfaces;

public interface IAuditLogWriter
{
    Task WriteAsync(
        Guid? tenantId,
        Guid? userId,
        string action,
        string entityName,
        Guid? entityId,
        string? newValues,
        string? ipAddress,
        string? userAgent);
}
