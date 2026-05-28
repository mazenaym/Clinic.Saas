using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;

namespace Clinic.Saas.Service.UseCases.Operations.Commands;

public class UpdateClinicSettingsCommand
{
    public class Command
    {
        public Guid TenantId { get; set; }
        public UpdateClinicSettingsDto Settings { get; set; } = null!;
    }

    public class Handler
    {
        private readonly IOperationsTenantRepository _repository;

        public Handler(IOperationsTenantRepository repository)
        {
            _repository = repository;
        }

        public async Task<BaseResponse<UpdateClinicSettingsDto>> Handle(Command command)
        {
            await _repository.UpsertSettingsAsync(command.TenantId, command.Settings);
            var settings = await _repository.GetSettingsAsync(command.TenantId);

            return new BaseResponse<UpdateClinicSettingsDto>
            {
                Success = true,
                Data = settings,
                Message = "OK",
                StatusCode = 200
            };
        }
    }
}
