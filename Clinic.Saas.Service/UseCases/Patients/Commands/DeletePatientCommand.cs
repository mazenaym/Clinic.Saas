using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Patients.Commands;

public class DeletePatientCommand
{
    public class Command
    {
        public Guid TenantId { get; set; }
        public Guid Id { get; set; }
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

            await _repository.DeleteAsync(command.TenantId, command.Id);
            return new BaseResponse<object>
            {
                Success = true,
                Message = "تم حذف المريض بنجاح",
                StatusCode = 200
            };
        }
    }
}
