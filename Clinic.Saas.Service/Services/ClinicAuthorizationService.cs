using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.Interfaces;

namespace Clinic.Saas.Service.Services;

public class ClinicAuthorizationService : IClinicAuthorizationService
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IVisitRepository _visitRepository;
    private readonly IPrescriptionRepository _prescriptionRepository;
    private readonly ICurrentUserService _currentUser;

    public ClinicAuthorizationService(
        IAppointmentRepository appointmentRepository,
        IVisitRepository visitRepository,
        IPrescriptionRepository prescriptionRepository,
        ICurrentUserService currentUser)
    {
        _appointmentRepository = appointmentRepository;
        _visitRepository = visitRepository;
        _prescriptionRepository = prescriptionRepository;
        _currentUser = currentUser;
    }

    public Task<bool> CanAccessDoctorScheduleAsync(Guid doctorId)
    {
        if (!HasTenantUser())
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(_currentUser.Role switch
        {
            UserRole.Admin or UserRole.Reception => true,
            UserRole.Doctor => _currentUser.UserId == doctorId,
            _ => false
        });
    }

    public async Task<bool> CanUpdateAppointmentAsync(Guid tenantId, Guid appointmentId)
    {
        if (!CanAccessTenant(tenantId))
        {
            return false;
        }

        if (_currentUser.Role is UserRole.Admin or UserRole.Reception)
        {
            return true;
        }

        if (_currentUser.Role != UserRole.Doctor)
        {
            return false;
        }

        var appointment = await _appointmentRepository.GetByIdAsync(tenantId, appointmentId);
        return appointment?.DoctorId == _currentUser.UserId;
    }

    public async Task<bool> CanViewVisitAsync(Guid tenantId, Guid visitId)
    {
        if (!CanAccessTenant(tenantId))
        {
            return false;
        }

        if (_currentUser.Role == UserRole.Admin)
        {
            return true;
        }

        if (_currentUser.Role != UserRole.Doctor)
        {
            return false;
        }

        var visit = await _visitRepository.GetByIdAsync(tenantId, visitId);
        return visit?.DoctorId == _currentUser.UserId;
    }

    public async Task<bool> CanViewPrescriptionAsync(Guid tenantId, Guid prescriptionId)
    {
        if (!CanAccessTenant(tenantId))
        {
            return false;
        }

        if (_currentUser.Role == UserRole.Admin)
        {
            return true;
        }

        if (_currentUser.Role != UserRole.Doctor)
        {
            return false;
        }

        var prescription = await _prescriptionRepository.GetByIdAsync(tenantId, prescriptionId);
        return prescription?.DoctorId == _currentUser.UserId;
    }

    private bool HasTenantUser() =>
        _currentUser.IsAuthenticated &&
        _currentUser.TenantId.HasValue &&
        _currentUser.UserId.HasValue;

    private bool CanAccessTenant(Guid tenantId) =>
        HasTenantUser() && _currentUser.TenantId == tenantId;
}
