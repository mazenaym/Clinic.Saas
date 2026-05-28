using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.UseCases.Procedures;

namespace Clinic.Saas.Service.UseCases.Procedures.Commands;

public class UpdateProcedureCommand
{
    public class Command
    {
        public Guid TenantId { get; set; }
        public Guid ProcedureId { get; set; }
        public UpdateProcedureDto Procedure { get; set; } = null!;
    }

    public class Handler
    {
        private readonly IProcedureRepository _procedureRepository;

        public Handler(IProcedureRepository procedureRepository)
        {
            _procedureRepository = procedureRepository;
        }

        public async Task<BaseResponse<ProcedureDto>> Handle(Command command)
        {
            var existing = await _procedureRepository.GetByIdAsync(command.TenantId, command.ProcedureId);
            if (existing is null)
            {
                return CreateProcedureCommand.Handler.Fail("Procedure not found.", 404);
            }

            var validation = await CreateProcedureCommand.Handler.ValidateAsync(
                _procedureRepository,
                command.TenantId,
                command.Procedure);
            if (validation is not null)
            {
                return validation;
            }

            var updated = await _procedureRepository.UpdateAsync(command.TenantId, new Procedure
            {
                Id = command.ProcedureId,
                CategoryId = command.Procedure.CategoryId,
                Name = command.Procedure.Name.Trim(),
                Specialty = string.IsNullOrWhiteSpace(command.Procedure.Specialty) ? null : command.Procedure.Specialty.Trim(),
                DefaultPrice = command.Procedure.DefaultPrice
            });

            if (!updated)
            {
                return CreateProcedureCommand.Handler.Fail("Procedure not found.", 404);
            }

            var refreshed = await _procedureRepository.GetByIdAsync(command.TenantId, command.ProcedureId);
            return new BaseResponse<ProcedureDto>
            {
                Success = true,
                Message = "Procedure updated.",
                StatusCode = 200,
                Data = ProcedureMapper.ToDto(refreshed ?? existing)
            };
        }
    }
}
