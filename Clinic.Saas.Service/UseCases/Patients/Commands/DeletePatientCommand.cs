using Clinic.Saas.Domain.Exceptions;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Patients.Commands;

public class DeletePatientCommand
{
    public class Command
    {
        public Guid TenantId { get; set; }
        public Guid Id { get; set; }
        public string? RowVersion { get; set; }
    }

    public class Handler
    {
        private readonly IPatientRepository _repository;

        public Handler(IPatientRepository repository)
        {
            _repository = repository;
        }

        public async Task<BaseResponse<object>> Handle(Command command)
        {
            var existing = await _repository.GetByIdAsync(command.TenantId, command.Id);
            if (existing is null)
            {
                return new BaseResponse<object>
                {
                    Success = false,
                    Message = "المريض غير موجود",
                    StatusCode = 404
                };
            }

            try
            {
                var rowVersion = string.IsNullOrWhiteSpace(command.RowVersion)
                    ? existing.RowVersion
                    : command.RowVersion.FromBase64RowVersion();
                await _repository.DeleteAsync(command.TenantId, command.Id, rowVersion);
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
                Message = "تم حذف المريض بنجاح",
                StatusCode = 200
            };
        }
    }
}
