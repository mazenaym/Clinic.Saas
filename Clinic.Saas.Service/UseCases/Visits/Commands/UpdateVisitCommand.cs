using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Exceptions;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using System.Text.Json;

namespace Clinic.Saas.Service.UseCases.Visits.Commands;

public class UpdateVisitCommand
{
    public class Command
    {
        public Guid TenantId { get; set; }
        public Guid VisitId { get; set; }
        public UpdateVisitDto Visit { get; set; } = null!;
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
                    Success = false,
                    Message = "Visit is finalized and cannot be updated.",
                    StatusCode = 409
                };
            }

            var entity = new Visit
            {
                VisitType = command.Visit.VisitType,
                ChiefComplaint = command.Visit.ChiefComplaint,
                VitalSigns = command.Visit.VitalSigns is null ? null : JsonSerializer.Serialize(command.Visit.VitalSigns),
                ClinicalNotes = command.Visit.ClinicalNotes,
                Diagnosis = command.Visit.Diagnosis,
                DiagnosisCode = command.Visit.DiagnosisCode,
                FollowUpDate = command.Visit.FollowUpDate,
                RowVersion = string.IsNullOrWhiteSpace(command.Visit.RowVersion)
                    ? visit.RowVersion
                    : command.Visit.RowVersion.FromBase64RowVersion()
            };

            int rows;
            try
            {
                rows = await _repository.UpdateClinicalDetailsAsync(command.TenantId, command.VisitId, entity);
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
                    Message = "Visit is finalized and cannot be updated.",
                    StatusCode = 409
                };
            }

            return new BaseResponse<object>
            {
                Success = true,
                Message = "Visit updated.",
                Data = true,
                StatusCode = 200
            };
        }
    }
}
