using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Appointments.Commands;

public class UpdateAppointmentStatusCommand
{
    public class Command
    {
        public UpdateAppointmentStatusDto Request { get; set; } = null!;
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
            var appointment = await _repository.GetByIdAsync(command.Request.Id);
            if (appointment is null)
            {
                return new BaseResponse<object>
                {
                    Success = false,
                    Message = "الموعد غير موجود",
                    StatusCode = 404
                };
            }

            await _repository.UpdateStatusAsync(command.Request.Id, command.Request.Status, command.Request.CancelReason);
            return new BaseResponse<object>
            {
                Success = true,
                Message = "تم تحديث حالة الموعد",
                StatusCode = 200
            };
        }
    }
}
