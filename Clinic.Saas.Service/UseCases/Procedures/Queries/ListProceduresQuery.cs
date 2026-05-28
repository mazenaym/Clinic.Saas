using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.UseCases.Procedures;

namespace Clinic.Saas.Service.UseCases.Procedures.Queries;

public class ListProceduresQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
        public bool IncludeInactive { get; set; }
    }

    public class Handler
    {
        private readonly IProcedureRepository _procedureRepository;

        public Handler(IProcedureRepository procedureRepository)
        {
            _procedureRepository = procedureRepository;
        }

        public async Task<BaseResponse<List<ProcedureDto>>> Handle(Query query)
        {
            var procedures = await _procedureRepository.ListAsync(query.TenantId, query.IncludeInactive);
            return new BaseResponse<List<ProcedureDto>>
            {
                Success = true,
                Message = "OK",
                StatusCode = 200,
                Data = procedures.Select(ProcedureMapper.ToDto).ToList()
            };
        }
    }
}
