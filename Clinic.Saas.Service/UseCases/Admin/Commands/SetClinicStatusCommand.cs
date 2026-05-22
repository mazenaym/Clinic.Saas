using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;

namespace Clinic.Saas.Service.UseCases.Admin.Commands;

public class SetClinicStatusCommand
{
    public class Command
    {
        public Guid ClinicId { get; set; }
        public bool IsActive { get; set; }
    }

    public class Handler
    {
        private readonly IPlatformAdminRepository _repository;

        public Handler(IPlatformAdminRepository repository)
        {
            _repository = repository;
        }

        public async Task<BaseResponse<AdminClinicDto>> Handle(Command command)
        {
            var current = await _repository.GetClinicByIdAsync(command.ClinicId);
            if (current is null)
            {
                return new BaseResponse<AdminClinicDto>
                {
                    Success = false,
                    Message = "Clinic was not found",
                    StatusCode = 404
                };
            }

            await _repository.SetClinicStatusAsync(command.ClinicId, command.IsActive);
            var updated = await _repository.GetClinicByIdAsync(command.ClinicId);
            return new BaseResponse<AdminClinicDto>
            {
                Success = true,
                Message = command.IsActive ? "Clinic activated successfully" : "Clinic deactivated successfully",
                Data = updated,
                StatusCode = 200
            };
        }
    }
}
