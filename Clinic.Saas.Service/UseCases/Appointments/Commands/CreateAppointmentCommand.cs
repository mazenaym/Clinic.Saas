using AutoMapper;
using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using FluentValidation;

namespace Clinic.Saas.Service.UseCases.Appointments.Commands;

public class CreateAppointmentCommand
{
    public class Command
    {
        public Guid TenantId { get; set; }
        public CreateAppointmentDto Appointment { get; set; } = null!;
    }

    public class Handler
    {
        private readonly IAppointmentRepository _repository;
        private readonly IValidator<CreateAppointmentDto> _validator;
        private readonly IMapper _mapper;

        public Handler(IAppointmentRepository repository, IValidator<CreateAppointmentDto> validator, IMapper mapper)
        {
            _repository = repository;
            _validator = validator;
            _mapper = mapper;
        }

        public async Task<BaseResponse<AppointmentDto>> Handle(Command command)
        {
            var validation = await _validator.ValidateAsync(command.Appointment);
            if (!validation.IsValid)
            {
                return new BaseResponse<AppointmentDto>
                {
                    Success = false,
                    Message = "بيانات الموعد غير صحيحة",
                    Errors = validation.Errors.Select(x => x.ErrorMessage).ToList(),
                    StatusCode = 400
                };
            }

            var hasConflict = await _repository.HasConflictAsync(
                command.TenantId,
                command.Appointment.DoctorId,
                command.Appointment.AppointmentDate,
                command.Appointment.StartTime,
                command.Appointment.EndTime);

            if (hasConflict)
            {
                return new BaseResponse<AppointmentDto>
                {
                    Success = false,
                    Message = "يوجد تعارض مع موعد آخر لنفس الدكتور",
                    StatusCode = 409
                };
            }

            var entity = new Appointment
            {
                TenantId = command.TenantId,
                PatientId = command.Appointment.PatientId,
                DoctorId = command.Appointment.DoctorId,
                AppointmentDate = command.Appointment.AppointmentDate.Date,
                StartTime = command.Appointment.StartTime,
                EndTime = command.Appointment.EndTime,
                Status = AppointmentStatus.Scheduled,
                Type = command.Appointment.Type,
                Source = command.Appointment.Source,
                Notes = command.Appointment.Notes,
                ReminderSent = false,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _repository.AddAsync(entity);
            return new BaseResponse<AppointmentDto>
            {
                Success = true,
                Message = "تم حجز الموعد بنجاح",
                Data = _mapper.Map<AppointmentDto>(created),
                StatusCode = 201
            };
        }
    }
}
