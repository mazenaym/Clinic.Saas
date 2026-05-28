using Clinic.Saas.Domain.Exceptions;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Appointments.Commands;

public class RescheduleAppointmentCommand
{
    public class Command
    {
        public Guid TenantId { get; set; }
        public Guid AppointmentId { get; set; }
        public RescheduleAppointmentDto Request { get; set; } = null!;
    }

    public class Handler
    {
        private readonly IAppointmentRepository _repository;

        public Handler(IAppointmentRepository repository)
        {
            _repository = repository;
        }

        public async Task<BaseResponse<object>> Handle(Command command)
        {
            var appointment = await _repository.GetByIdAsync(command.TenantId, command.AppointmentId);
            if (appointment is null)
            {
                return new BaseResponse<object>
                {
                    Success = false,
                    Message = "Appointment not found.",
                    StatusCode = 404
                };
            }

            var hasConflict = await _repository.HasConflictAsync(
                command.TenantId,
                appointment.DoctorId,
                command.Request.AppointmentDate.Date,
                command.Request.StartTime,
                command.Request.EndTime,
                command.AppointmentId);

            if (hasConflict)
            {
                return new BaseResponse<object>
                {
                    Success = false,
                    Message = "Appointment conflicts with another booking.",
                    StatusCode = 409
                };
            }

            appointment.AppointmentDate = command.Request.AppointmentDate.Date;
            appointment.StartTime = command.Request.StartTime;
            appointment.EndTime = command.Request.EndTime;
            appointment.RowVersion = string.IsNullOrWhiteSpace(command.Request.RowVersion)
                ? appointment.RowVersion
                : command.Request.RowVersion.FromBase64RowVersion();

            try
            {
                await _repository.UpdateAsync(command.TenantId, appointment);
            }
            catch (ConcurrencyConflictException ex)
            {
                return new BaseResponse<object>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 409
                };
            }
            catch (RecordNotFoundException ex)
            {
                return new BaseResponse<object>
                {
                    Success = false,
                    Message = ex.Message,
                    StatusCode = 404
                };
            }

            return new BaseResponse<object>
            {
                Success = true,
                Message = "Appointment rescheduled.",
                Data = true,
                StatusCode = 200
            };
        }
    }
}
