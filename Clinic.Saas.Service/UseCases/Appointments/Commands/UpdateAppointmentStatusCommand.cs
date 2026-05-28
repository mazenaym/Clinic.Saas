using Clinic.Saas.Domain.Exceptions;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Appointments.Commands;

public class UpdateAppointmentStatusCommand
{
    public class Command
    {
        public Guid TenantId { get; set; }
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
            var existing = await _repository.GetByIdAsync(command.TenantId, command.Request.Id);
            if (existing is null)
            {
                return new BaseResponse<object>
                {
                    Success = false,
                    Message = "Ø§Ù„Ù…ÙˆØ¹Ø¯ ØºÙŠØ± Ù…ÙˆØ¬ÙˆØ¯",
                    StatusCode = 404
                };
            }

            bool updated;
            try
            {
                var rowVersion = string.IsNullOrWhiteSpace(command.Request.RowVersion)
                    ? existing.RowVersion
                    : command.Request.RowVersion.FromBase64RowVersion();

                updated = await _repository.UpdateStatusAsync(
                    command.TenantId,
                    command.Request.Id,
                    command.Request.Status,
                    command.Request.CancelReason,
                    rowVersion);
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

            if (!updated)
            {
                return new BaseResponse<object>
                {
                    Success = false,
                    Message = "الموعد غير موجود",
                    StatusCode = 404
                };
            }

            return new BaseResponse<object>
            {
                Success = true,
                Message = "تم تحديث حالة الموعد",
                StatusCode = 200
            };
        }
    }
}
