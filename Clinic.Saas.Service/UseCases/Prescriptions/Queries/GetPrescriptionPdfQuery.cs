using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using System.Text;

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

        public Handler(IPrescriptionRepository repository)
        {
            _repository = repository;
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

            var body = $"Prescription\nPatient: {prescription.PatientName}\nDoctor: {prescription.DoctorName}\nDate: {prescription.CreatedAt:yyyy-MM-dd}\n\n" +
                string.Join("\n", prescription.Items.Select(i => $"- {i.DrugName} {i.Dosage} {i.Frequency} {i.Duration} {i.Instructions}"));

            return new BaseResponse<PrescriptionPdfDto>
            {
                Success = true,
                Data = new PrescriptionPdfDto
                {
                    Content = CreateSimplePdf(body),
                    ContentType = "application/pdf",
                    FileName = $"prescription-{query.PrescriptionId}.pdf"
                },
                StatusCode = 200
            };
        }

        private static byte[] CreateSimplePdf(string text)
        {
            var escaped = text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("\r", "").Replace("\n", ") Tj T* (");
            var stream = $"BT /F1 12 Tf 50 780 Td ({escaped}) Tj ET";
            var pdf = $@"%PDF-1.4
1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj
2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj
3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >> endobj
4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj
5 0 obj << /Length {stream.Length} >> stream
{stream}
endstream endobj
xref
0 6
0000000000 65535 f 
trailer << /Root 1 0 R /Size 6 >>
startxref
0
%%EOF";
            return Encoding.ASCII.GetBytes(pdf);
        }
    }
}
