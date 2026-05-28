using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Reports.Queries;

public class GetFinancialDuesReportQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public Guid? DoctorId { get; set; }
    }

    public class Handler
    {
        private readonly IInvoiceRepository _invoiceRepository;

        public Handler(IInvoiceRepository invoiceRepository)
        {
            _invoiceRepository = invoiceRepository;
        }

        public async Task<BaseResponse<FinancialDuesReportDto>> Handle(Query query)
        {
            var toExclusive = query.To?.Date.AddDays(1);
            var data = await _invoiceRepository.GetFinancialDuesAsync(
                query.TenantId,
                query.From?.Date,
                toExclusive,
                query.DoctorId);

            return new BaseResponse<FinancialDuesReportDto>
            {
                Success = true,
                Message = "OK",
                StatusCode = 200,
                Data = new FinancialDuesReportDto
                {
                    Summary = new FinancialDuesSummaryDto
                    {
                        TotalOutstanding = data.Summary.TotalOutstanding,
                        TotalPaid = data.Summary.TotalPaid,
                        PatientsWithDebtCount = data.Summary.PatientsWithDebtCount
                    },
                    Patients = data.Patients.Select(row => new FinancialDuesPatientDto
                    {
                        PatientId = row.PatientId,
                        PatientName = row.PatientName,
                        Phone = row.Phone,
                        TotalAmount = row.TotalAmount,
                        PaidAmount = row.PaidAmount,
                        OutstandingAmount = row.OutstandingAmount,
                        LastPaymentDate = row.LastPaymentDate
                    }).ToList()
                }
            };
        }
    }
}
