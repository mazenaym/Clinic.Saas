using Clinic.Saas.Domain.Exceptions;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Visits.Commands;

public class FinalizeVisitCommand
{
    public class Command
    {
        public Guid TenantId { get; set; }
        public Guid VisitId { get; set; }
        public Guid FinalizedByUserId { get; set; }
        public string? RowVersion { get; set; }
    }

    public class Handler
    {
        private readonly IVisitRepository _repository;

        public Handler(IVisitRepository repository)
        {
            _repository = repository;
        }

        public async Task<BaseResponse<object>> Handle(Command command)
        {
            var visit = await _repository.GetByIdAsync(command.TenantId, command.VisitId);
            if (visit is null)
            {
                return new BaseResponse<object>
                {
                    Success = false,
                    Message = "Visit not found.",
                    StatusCode = 404
                };
            }

            if (visit.FinalizedAt.HasValue)
            {
                return new BaseResponse<object>
                {
                    Success = true,
                    Message = "Visit finalized.",
                    Data = true,
                    StatusCode = 200
                };
            }

            int rows;
            try
            {
                rows = await _repository.FinalizeAsync(
                    command.TenantId,
                    command.VisitId,
                    command.FinalizedByUserId,
                    string.IsNullOrWhiteSpace(command.RowVersion)
                        ? visit.RowVersion
                        : command.RowVersion.FromBase64RowVersion());
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
            if (rows == 0)
            {
                return new BaseResponse<object>
                {
                    Success = false,
                    Message = "Visit not found.",
                    StatusCode = 404
                };
            }

            return new BaseResponse<object>
            {
                Success = true,
                Message = "Visit finalized.",
                Data = true,
                StatusCode = 200
            };
        }
    }
}
