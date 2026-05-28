using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Procedures.Commands;

public class SetProcedureActiveCommand
{
    public class Command
    {
        public Guid TenantId { get; set; }
        public Guid ProcedureId { get; set; }
        public bool IsActive { get; set; }
    }

    public class Handler
    {
        private readonly IProcedureRepository _procedureRepository;

        public Handler(IProcedureRepository procedureRepository)
        {
            _procedureRepository = procedureRepository;
        }

        public async Task<BaseResponse<bool>> Handle(Command command)
        {
            var updated = await _procedureRepository.SetActiveAsync(
                command.TenantId,
                command.ProcedureId,
                command.IsActive);

            if (!updated)
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = "Procedure not found.",
                    StatusCode = 404
                };
            }

            return new BaseResponse<bool>
            {
                Success = true,
                Message = command.IsActive ? "Procedure activated." : "Procedure deactivated.",
                Data = true,
                StatusCode = 200
            };
        }
    }
}
