using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.UseCases.Procedures;

namespace Clinic.Saas.Service.UseCases.Procedures.Commands;

public class CreateProcedureCommand
{
    public class Command
    {
        public Guid TenantId { get; set; }
        public CreateProcedureDto Procedure { get; set; } = null!;
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
            var validation = await ValidateAsync(_procedureRepository, command.TenantId, command.Procedure);
            if (validation is not null)
            {
                return validation;
            }

            var created = await _procedureRepository.AddAsync(new Procedure
            {
                TenantId = command.TenantId,
                CategoryId = command.Procedure.CategoryId,
                Name = command.Procedure.Name.Trim(),
                Specialty = string.IsNullOrWhiteSpace(command.Procedure.Specialty) ? null : command.Procedure.Specialty.Trim(),
                DefaultPrice = command.Procedure.DefaultPrice,
                IsActive = true
            });

            return new BaseResponse<ProcedureDto>
            {
                Success = true,
                Message = "Procedure created.",
                StatusCode = 200,
                Data = ProcedureMapper.ToDto(created)
            };
        }

        internal static async Task<BaseResponse<ProcedureDto>?> ValidateAsync(
            IProcedureRepository repository,
            Guid tenantId,
            CreateProcedureDto procedure)
        {
            if (string.IsNullOrWhiteSpace(procedure.Name))
            {
                return Fail("Procedure name is required.", 400);
            }

            if (procedure.DefaultPrice < 0)
            {
                return Fail("Default price cannot be negative.", 400);
            }

            if (procedure.CategoryId.HasValue &&
                !await repository.CategoryExistsAsync(tenantId, procedure.CategoryId.Value))
            {
                return Fail("Procedure category not found.", 404);
            }

            return null;
        }

        internal static BaseResponse<ProcedureDto> Fail(string message, int statusCode) => new()
        {
            Success = false,
            Message = message,
            Errors = [message],
            StatusCode = statusCode
        };
    }
}
