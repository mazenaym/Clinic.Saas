using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;

namespace Clinic.Saas.Service.UseCases.Prescriptions.Queries;

public class GetPrescriptionPdfQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
        public Guid PrescriptionId { get; set; }
    }

    public class Handler
    {
        private readonly IPrescriptionRepository _repository;
        private readonly IPdfDocumentService _pdf;

        public Handler(IPrescriptionRepository repository, IPdfDocumentService pdf)
        {
            _repository = repository;
            _pdf = pdf;
        }

        public async Task<BaseResponse<PrescriptionPdfDto>> Handle(Query query)
        {
            var prescription = await _repository.GetByIdAsync(query.TenantId, query.PrescriptionId);
            if (prescription is null)
            {
                return new BaseResponse<PrescriptionPdfDto>
                {
                    Success = false,
                    Message = "Prescription not found.",
                    StatusCode = 404
                };
            }

            return new BaseResponse<PrescriptionPdfDto>
            {
                Success = true,
                Data = new PrescriptionPdfDto
                {
                    Content = _pdf.Generate("روشتة طبية", [
                        ("المريض", prescription.PatientName),
                        ("الطبيب", prescription.DoctorName),
                        ("التاريخ", prescription.CreatedAt.ToString("yyyy-MM-dd"))],
                        prescription.Items.Select(i => $"{i.DrugName} — {i.Dosage} — {i.Frequency} — {i.Duration} — {i.Instructions}")),
                    ContentType = "application/pdf",
                    FileName = $"prescription-{query.PrescriptionId}.pdf"
                },
                StatusCode = 200
            };
        }

    }
}
