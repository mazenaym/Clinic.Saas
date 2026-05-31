using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;

namespace Clinic.Saas.Service.UseCases.Appointments.Queries;

public class GetDoctorAppointmentScheduleQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
        public DateTime Date { get; set; }
        public Guid? DoctorId { get; set; }
    }

    public class Handler
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IUserRepository _userRepository;
        private readonly IOperationsTenantRepository _tenantRepository;

        public Handler(
            IAppointmentRepository appointmentRepository,
            IUserRepository userRepository,
            IOperationsTenantRepository tenantRepository)
        {
            _appointmentRepository = appointmentRepository;
            _userRepository = userRepository;
            _tenantRepository = tenantRepository;
        }

        public async Task<BaseResponse<AppointmentScheduleDto>> Handle(Query query)
        {
            var settings = await _tenantRepository.GetSettingsAsync(query.TenantId);
            var doctors = (await _userRepository.GetByTenantAsync(query.TenantId))
                .Where(user => user.Role == UserRole.Doctor && user.IsActive)
                .OrderBy(user => user.FullName)
                .ToList();

            if (query.DoctorId.HasValue)
            {
                doctors = doctors.Where(user => user.Id == query.DoctorId.Value).ToList();
            }

            var doctorIds = doctors.Select(user => user.Id).ToHashSet();
            var appointments = (await _appointmentRepository.GetByDateAsync(query.TenantId, query.Date))
                .Where(appointment => doctorIds.Contains(appointment.DoctorId))
                .OrderBy(appointment => appointment.StartTime)
                .ThenBy(appointment => appointment.DoctorName)
                .ToList();

            return new BaseResponse<AppointmentScheduleDto>
            {
                Success = true,
                Message = "OK",
                StatusCode = 200,
                Data = new AppointmentScheduleDto
                {
                    Date = query.Date.Date,
                    OpenTime = settings.OpenTime,
                    CloseTime = settings.CloseTime,
                    SlotDurationMin = settings.SlotDurationMin,
                    Doctors = doctors.Select(doctor => new ScheduleDoctorDto
                    {
                        Id = doctor.Id,
                        FullName = doctor.FullName,
                        Specialty = doctor.Specialty
                    }).ToList(),
                    Appointments = appointments.Select(appointment => new ScheduleAppointmentDto
                    {
                        Id = appointment.Id,
                        DoctorId = appointment.DoctorId,
                        PatientId = appointment.PatientId,
                        StartTime = appointment.StartTime,
                        EndTime = appointment.EndTime,
                        Status = appointment.Status.ToString(),
                        Type = appointment.Type.ToString(),
                        PatientName = appointment.PatientName,
                        PatientPhone = appointment.PatientPhone,
                        Notes = appointment.Notes,
                        RowVersion = appointment.RowVersion.ToBase64RowVersion()
                    }).ToList()
                }
            };
        }
    }
}
