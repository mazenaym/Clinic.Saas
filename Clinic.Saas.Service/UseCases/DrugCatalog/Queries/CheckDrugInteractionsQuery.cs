using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.DrugCatalog.Queries;

public class CheckDrugInteractionsQuery
{
    public class Query
    {
        public string[] DrugNames { get; set; } = [];
    }

    public class Handler
    {
        private readonly IDrugCatalogRepository _repository;

        public Handler(IDrugCatalogRepository repository)
        {
            _repository = repository;
        }

        public async Task<BaseResponse<List<DrugInteractionWarningDto>>> Handle(Query query)
        {
            var drugs = await _repository.GetByTradeNamesAsync(query.DrugNames);
            var warnings = drugs
                .Where(x => !string.IsNullOrWhiteSpace(x.Interactions))
                .Select(x => new DrugInteractionWarningDto
                {
                    Drug = x.TradeName,
                    Warning = x.Interactions!
                })
                .ToList();

            return new BaseResponse<List<DrugInteractionWarningDto>>
            {
                Success = true,
                Message = "OK",
                Data = warnings,
                StatusCode = 200
            };
        }
    }
}
