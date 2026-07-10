using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.UseCases.Payments.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Clinic.Saas.Tests;

public class PaymentCreationTests
{
    [Theory]
    [InlineData(500, 0, 500, 0, PaymentStatus.Paid)]
    [InlineData(500, 0, 300, 200, PaymentStatus.Partial)]
    [InlineData(500, 50, 300, 150, PaymentStatus.Partial)]
    public async Task Create_payment_calculates_status_and_remaining_amount(
        decimal total, decimal discount, decimal paid, decimal remaining, PaymentStatus status)
    {
        var repository = new CapturingPaymentRepository();
        var handler = CreateHandler(repository);

        var result = await handler.Handle(new CreatePaymentCommand.Command
        {
            TenantId = Guid.NewGuid(),
            Payment = ValidPayment(total, discount, paid)
        });

        Assert.True(result.Success);
        Assert.Equal(201, result.StatusCode);
        Assert.NotNull(repository.Created);
        Assert.Equal(remaining, repository.Created.RemainingAmount);
        Assert.Equal(status, repository.Created.Status);
    }

    [Fact]
    public async Task Create_payment_rejects_overpayment_with_structured_bad_request()
    {
        var repository = new CapturingPaymentRepository();
        var handler = CreateHandler(repository);

        var result = await handler.Handle(new CreatePaymentCommand.Command
        {
            TenantId = Guid.NewGuid(),
            Payment = ValidPayment(500, 0, 501)
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains(result.Errors!, error => error.Contains("المبلغ المدفوع"));
        Assert.Null(repository.Created);
    }

    private static CreatePaymentCommand.Handler CreateHandler(IPaymentRepository repository)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddApplication();
        services.AddSingleton(repository);
        return services.BuildServiceProvider().GetRequiredService<CreatePaymentCommand.Handler>();
    }

    private static CreatePaymentDto ValidPayment(decimal total, decimal discount, decimal paid) => new()
    {
        VisitId = Guid.NewGuid(),
        PatientId = Guid.NewGuid(),
        TotalAmount = total,
        DiscountAmount = discount,
        PaidAmount = paid,
        PaymentMethod = PaymentMethod.Cash,
        Items =
        [
            new CreatePaymentItemDto
            {
                ServiceName = "كشف",
                ServiceType = ServiceType.Consultation,
                Quantity = 1,
                UnitPrice = total
            }
        ]
    };

    private sealed class CapturingPaymentRepository : IPaymentRepository
    {
        public Payment? Created { get; private set; }
        public Task<Payment> AddAsync(Payment entity) { Created = entity; return Task.FromResult(entity); }
        public Task<Payment?> GetByIdAsync(Guid tenantId, Guid id) => Task.FromResult<Payment?>(null);
        public Task<IEnumerable<Payment>> GetByPatientAsync(Guid tenantId, Guid patientId) => Task.FromResult(Enumerable.Empty<Payment>());
        public Task UpdateAsync(Guid tenantId, Payment entity) => Task.CompletedTask;
        public Task<bool> UpdateWithItemsAsync(Guid tenantId, Payment entity) => Task.FromResult(false);
        public Task<bool> RefundAsync(Guid tenantId, Guid id, string? reason, byte[] rowVersion) => Task.FromResult(false);
        public Task DeleteAsync(Guid tenantId, Guid id, byte[] rowVersion) => Task.CompletedTask;
        public Task<string> GenerateInvoiceNumberAsync(Guid tenantId, DateTime createdAt) => Task.FromResult("INV-TEST");
        public Task<IEnumerable<Payment>> GetByDateAsync(Guid tenantId, DateTime date) => Task.FromResult(Enumerable.Empty<Payment>());
        public Task<IEnumerable<PaymentDebtRow>> GetDebtTrackingAsync(Guid tenantId) => Task.FromResult(Enumerable.Empty<PaymentDebtRow>());
        public Task<IEnumerable<MonthlyRevenueRow>> GetMonthlyRevenueAsync(Guid tenantId, DateTime start, DateTime end) => Task.FromResult(Enumerable.Empty<MonthlyRevenueRow>());
        public Task<Payment?> GetByIdAsync(Guid id) => Task.FromResult<Payment?>(null);
        public Task<IEnumerable<Payment>> GetAllAsync() => Task.FromResult(Enumerable.Empty<Payment>());
        public Task UpdateAsync(Payment entity) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
    }
}
