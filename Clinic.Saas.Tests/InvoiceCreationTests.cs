using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.UseCases.Invoices.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Clinic.Saas.Tests;

public class InvoiceCreationTests
{
    [Theory]
    [InlineData(500, 0, 0, 500, InvoiceStatus.Draft)]
    [InlineData(500, 0, 300, 200, InvoiceStatus.PartiallyPaid)]
    [InlineData(500, 50, 450, 0, InvoiceStatus.Paid)]
    public async Task Create_invoice_calculates_status_and_remaining_amount(
        decimal subtotal, decimal discount, decimal paid, decimal remaining, InvoiceStatus status)
    {
        var repository = new CapturingInvoiceRepository();
        var handler = CreateHandler(repository);

        var result = await handler.Handle(new CreateInvoiceCommand.Command
        {
            TenantId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Invoice = ValidInvoice(subtotal, discount, paid)
        });

        Assert.True(result.Success);
        Assert.NotNull(repository.Created);
        Assert.Equal(remaining, repository.Created.RemainingAmount);
        Assert.Equal(status, repository.Created.Status);
    }

    [Fact]
    public async Task Create_invoice_rejects_empty_items()
    {
        var repository = new CapturingInvoiceRepository();
        var handler = CreateHandler(repository);

        var result = await handler.Handle(new CreateInvoiceCommand.Command
        {
            TenantId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Invoice = new CreateInvoiceDto
            {
                PatientId = Guid.NewGuid(),
                Items = new List<CreateInvoiceItemDto>()
            }
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Null(repository.Created);
    }

    [Fact]
    public async Task Create_invoice_rejects_empty_patient()
    {
        var repository = new CapturingInvoiceRepository();
        var handler = CreateHandler(repository);

        var result = await handler.Handle(new CreateInvoiceCommand.Command
        {
            TenantId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Invoice = new CreateInvoiceDto
            {
                PatientId = Guid.Empty,
                Items = new List<CreateInvoiceItemDto>
                {
                    new() { Description = "Consultation", Quantity = 1, UnitPrice = 500 }
                }
            }
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Null(repository.Created);
    }

    [Fact]
    public async Task Create_invoice_calculates_line_totals_correctly()
    {
        var repository = new CapturingInvoiceRepository();
        var handler = CreateHandler(repository);

        var result = await handler.Handle(new CreateInvoiceCommand.Command
        {
            TenantId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Invoice = ValidInvoice(500, 0, 0)
        });

        Assert.True(result.Success);
        Assert.NotNull(repository.Created);
        Assert.Single(repository.Created.Items);
        Assert.Equal(500, repository.Created.Items.First().LineTotal);
        Assert.Equal(500, repository.Created.Subtotal);
        Assert.Equal(500, repository.Created.GrandTotal);
    }

    private static CreateInvoiceCommand.Handler CreateHandler(IInvoiceRepository repository)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddApplication();
        services.AddSingleton(repository);
        return services.BuildServiceProvider().GetRequiredService<CreateInvoiceCommand.Handler>();
    }

    private static CreateInvoiceDto ValidInvoice(decimal subtotal, decimal discount, decimal paid) => new()
    {
        PatientId = Guid.NewGuid(),
        VisitId = Guid.NewGuid(),
        Notes = "Test invoice",
        Items =
        [
            new CreateInvoiceItemDto
            {
                Description = "Consultation",
                ServiceType = ServiceType.Consultation,
                Quantity = 1,
                UnitPrice = subtotal,
                DiscountAmount = discount,
                TaxAmount = 0
            }
        ]
    };

    private sealed class CapturingInvoiceRepository : IInvoiceRepository
    {
        public Invoice? Created { get; private set; }
        public Task<Invoice> AddAsync(Invoice invoice) { Created = invoice; return Task.FromResult(invoice); }
        public Task<Invoice?> GetByIdAsync(Guid tenantId, Guid id) => Task.FromResult<Invoice?>(null);
        public Task<Invoice?> AddPaymentAsync(Guid tenantId, Guid invoiceId, InvoicePayment payment) => Task.FromResult<Invoice?>(null);
        public Task<PatientFinancialLedgerData> GetPatientLedgerAsync(Guid tenantId, Guid patientId) => Task.FromResult(new PatientFinancialLedgerData());
        public Task<FinancialDuesReportData> GetFinancialDuesAsync(Guid tenantId, DateTime? from, DateTime? toExclusive, Guid? doctorId) => Task.FromResult(new FinancialDuesReportData());
        public Task<string> GenerateInvoiceNumberAsync(Guid tenantId, DateTime createdAt) => Task.FromResult("INV-TEST");
        public Task<IEnumerable<Invoice>> GetByPatientAsync(Guid tenantId, Guid patientId) => Task.FromResult(Enumerable.Empty<Invoice>());
        public Task<IEnumerable<Invoice>> GetByDateAsync(Guid tenantId, DateTime date) => Task.FromResult(Enumerable.Empty<Invoice>());
        public Task UpdateAsync(Guid tenantId, Invoice entity) => Task.CompletedTask;
        public Task<bool> UpdateWithItemsAsync(Guid tenantId, Invoice entity) => Task.FromResult(false);
        public Task<bool> RefundAsync(Guid tenantId, Guid id, string? reason, byte[] rowVersion) => Task.FromResult(false);
        public Task DeleteAsync(Guid tenantId, Guid id, byte[] rowVersion) => Task.CompletedTask;
        public Task<IEnumerable<InvoiceDebtRow>> GetDebtTrackingAsync(Guid tenantId) => Task.FromResult(Enumerable.Empty<InvoiceDebtRow>());
        public Task<IEnumerable<MonthlyRevenueRow>> GetMonthlyRevenueAsync(Guid tenantId, DateTime start, DateTime end) => Task.FromResult(Enumerable.Empty<MonthlyRevenueRow>());
        public Task<IEnumerable<DailyPaymentMethodTotal>> GetDailyPaymentMethodTotalsAsync(Guid tenantId, DateTime date) => Task.FromResult(Enumerable.Empty<DailyPaymentMethodTotal>());
    }
}
