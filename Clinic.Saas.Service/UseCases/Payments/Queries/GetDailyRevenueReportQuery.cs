using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Payments.Queries;

public class GetDailyRevenueReportQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
        public DateTime Date { get; set; }
    }

    public class Handler
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IVisitRepository _visitRepository;

        public Handler(IPaymentRepository paymentRepository, IAppointmentRepository appointmentRepository, IVisitRepository visitRepository)
        {
            _paymentRepository = paymentRepository;
            _appointmentRepository = appointmentRepository;
            _visitRepository = visitRepository;
        }

        public async Task<BaseResponse<DailyRevenueReportDto>> Handle(Query query)
        {
            var payments = (await _paymentRepository.GetByDateAsync(query.TenantId, query.Date)).ToList();
            var appointments = (await _appointmentRepository.GetByDateAsync(query.TenantId, query.Date)).ToList();
            var visits = (await _visitRepository.GetAllAsync()).Where(x => x.VisitDate.Date == query.Date.Date).ToList();

            var report = new DailyRevenueReportDto
            {
                Date = query.Date.Date,
                TotalAppointments = appointments.Count,
                CompletedVisits = visits.Count,
                GrossRevenue = payments.Sum(x => x.TotalAmount + x.TaxAmount),
                TotalDiscounts = payments.Sum(x => x.DiscountAmount),
                NetRevenue = payments.Sum(x => x.PaidAmount),
                CashPayments = payments.Where(x => x.PaymentMethod == PaymentMethod.Cash).Sum(x => x.PaidAmount),
                CardPayments = payments.Where(x => x.PaymentMethod == PaymentMethod.Card).Sum(x => x.PaidAmount),
                InsurancePayments = payments.Where(x => x.PaymentMethod == PaymentMethod.Insurance).Sum(x => x.PaidAmount)
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
