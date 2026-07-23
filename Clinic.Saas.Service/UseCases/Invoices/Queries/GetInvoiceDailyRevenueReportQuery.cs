using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Invoices.Queries;

public class GetInvoiceDailyRevenueReportQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
        public DateTime Date { get; set; }
    }

    public class Handler
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IVisitRepository _visitRepository;

        public Handler(IInvoiceRepository invoiceRepository, IAppointmentRepository appointmentRepository, IVisitRepository visitRepository)
        {
            _invoiceRepository = invoiceRepository;
            _appointmentRepository = appointmentRepository;
            _visitRepository = visitRepository;
        }

        public async Task<BaseResponse<DailyRevenueReportDto>> Handle(Query query)
        {
            var invoices = (await _invoiceRepository.GetByDateAsync(query.TenantId, query.Date)).ToList();
            var appointments = (await _appointmentRepository.GetByDateAsync(query.TenantId, query.Date)).ToList();
            var completedVisits = await _visitRepository.CountByDateAsync(query.TenantId, query.Date);
            var paymentMethodTotals = (await _invoiceRepository.GetDailyPaymentMethodTotalsAsync(query.TenantId, query.Date))
                .ToDictionary(x => (PaymentMethod)x.PaymentMethod, x => x.TotalAmount);

            var report = new DailyRevenueReportDto
            {
                Date = query.Date.Date,
                TotalAppointments = appointments.Count,
                CompletedVisits = completedVisits,
                GrossRevenue = invoices.Sum(x => x.GrandTotal),
                TotalDiscounts = invoices.Sum(x => x.DiscountAmount),
                NetRevenue = invoices.Sum(x => x.PaidAmount),
                CashPayments = paymentMethodTotals.GetValueOrDefault(PaymentMethod.Cash),
                CardPayments = paymentMethodTotals.GetValueOrDefault(PaymentMethod.Card),
                InsurancePayments = paymentMethodTotals.GetValueOrDefault(PaymentMethod.Insurance)
            };

            return new BaseResponse<DailyRevenueReportDto>
            {
                Success = true,
                Data = report,
                StatusCode = 200
            };
        }
    }
}
