using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.DrugCatalog.Queries;

public class SearchDrugsQuery
{
    public class Query
    {
        public string? Term { get; set; }
    }

    public class Handler
    {
        private readonly IDrugCatalogRepository _repository;

        public Handler(IDrugCatalogRepository repository)
        {
            _repository = repository;
        }

        public async Task<BaseResponse<List<DrugCatalogItemDto>>> Handle(Query query)
        {
            var drugs = await _repository.SearchAsync(query.Term);

            return new BaseResponse<List<DrugCatalogItemDto>>
            {
                Success = true,
                Message = "OK",
                Data = drugs.Select(x => new DrugCatalogItemDto
                {
                    Id = x.Id,
                    TradeName = x.TradeName,
                    GenericName = x.GenericName,
                    Strength = x.Strength,
                    Form = x.Form,
                    Interactions = x.Interactions
                }).ToList(),
                StatusCode = 200
            };
        }
    }
}
