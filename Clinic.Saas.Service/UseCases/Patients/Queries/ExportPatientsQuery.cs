using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using System.Text;

namespace Clinic.Saas.Service.UseCases.Patients.Queries;

public class ExportPatientsQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
    }

    public class Handler
    {
        private readonly IPatientRepository _repository;

        public Handler(IPatientRepository repository)
        {
            _repository = repository;
        }

        public async Task<BaseResponse<CsvFileDto>> Handle(Query query)
        {
            var rows = await _repository.GetExportRowsAsync(query.TenantId);

            var csv = new StringBuilder();
            csv.AppendLine("PatientCode,FullName,PhoneNumber,NationalId,Email,Gender,CreatedAt");
            foreach (var row in rows)
            {
                csv.AppendLine($"{Csv(row.PatientCode)},{Csv(row.FullName)},{Csv(row.PhoneNumber)},{Csv(row.NationalId)},{Csv(row.Email)},{row.Gender},{row.CreatedAt:O}");
            }

            return new BaseResponse<CsvFileDto>
            {
                Success = true,
                Message = "OK",
                Data = new CsvFileDto
                {
                    Content = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(),
                    FileName = "patients-export.csv"
                },
                StatusCode = 200
            };
        }

        private static string Csv(object? value)
        {
            var text = value?.ToString() ?? string.Empty;
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }
    }
}
