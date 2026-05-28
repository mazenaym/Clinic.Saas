using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Patients.Queries;

public class GetPatientFinancialLedgerQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
        public Guid PatientId { get; set; }
    }

    public class Handler
    {
        private readonly IPatientRepository _patientRepository;
        private readonly IInvoiceRepository _invoiceRepository;

        public Handler(IPatientRepository patientRepository, IInvoiceRepository invoiceRepository)
        {
            _patientRepository = patientRepository;
            _invoiceRepository = invoiceRepository;
        }

        public async Task<BaseResponse<PatientFinancialLedgerDto>> Handle(Query query)
        {
            var patient = await _patientRepository.GetByIdAsync(query.TenantId, query.PatientId);
            if (patient is null)
            {
                return new BaseResponse<PatientFinancialLedgerDto>
                {
                    Success = false,
                    Message = "Patient not found.",
                    StatusCode = 404
                };
            }

            var ledger = await _invoiceRepository.GetPatientLedgerAsync(query.TenantId, query.PatientId);
            return new BaseResponse<PatientFinancialLedgerDto>
            {
                Success = true,
                Message = "OK",
                StatusCode = 200,
                Data = new PatientFinancialLedgerDto
                {
                    Summary = new PatientFinancialLedgerSummaryDto
                    {
                        TotalInvoiced = ledger.Summary.TotalInvoiced,
                        TotalPaid = ledger.Summary.TotalPaid,
                        OutstandingBalance = ledger.Summary.OutstandingBalance
                    },
                    Entries = ledger.Entries.Select(entry => new PatientFinancialLedgerEntryDto
                    {
                        Date = entry.Date,
                        Type = entry.Type,
                        ReferenceNumber = entry.ReferenceNumber,
                        Description = entry.Description,
                        Debit = entry.Debit,
                        Credit = entry.Credit,
                        Balance = entry.Balance
                    }).ToList()
                }
            };
        }
    }
}
