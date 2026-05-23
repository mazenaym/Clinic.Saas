namespace Clinic.Saas.Service.Interfaces;

public interface IClinicAuthorizationService
{
    Task<bool> CanAccessDoctorScheduleAsync(Guid doctorId);
    Task<bool> CanUpdateAppointmentAsync(Guid tenantId, Guid appointmentId);
    Task<bool> CanViewVisitAsync(Guid tenantId, Guid visitId);
    Task<bool> CanViewPrescriptionAsync(Guid tenantId, Guid prescriptionId);
}
