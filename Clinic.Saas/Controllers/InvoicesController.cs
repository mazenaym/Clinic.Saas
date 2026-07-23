using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.Security;
using Clinic.Saas.Service.UseCases.Invoices.Commands;
using Clinic.Saas.Service.UseCases.Invoices.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/invoices")]
[ApiController]
[Authorize(Roles = "Admin,Reception")]
public class InvoicesController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;
    private readonly CreateInvoiceCommand.Handler _createInvoice;
    private readonly GetInvoiceByIdQuery.Handler _getInvoiceById;
    private readonly AddInvoicePaymentCommand.Handler _addInvoicePayment;
    private readonly GetInvoicePdfQuery.Handler _getInvoicePdf;
    private readonly UpdateInvoiceCommand.Handler _updateInvoice;
    private readonly RefundInvoiceCommand.Handler _refundInvoice;
    private readonly GetPatientInvoicesQuery.Handler _getPatientInvoices;
    private readonly GetInvoiceDebtTrackingQuery.Handler _debtTracking;
    private readonly GetInvoiceMonthlyRevenueQuery.Handler _monthlyRevenue;
    private readonly GetInvoiceDailyRevenueReportQuery.Handler _dailyRevenue;
    private readonly GetReceiptPdfQuery.Handler _receiptPdf;

    public InvoicesController(
        ICurrentUserService currentUser,
        IAuditService auditService,
        CreateInvoiceCommand.Handler createInvoice,
        GetInvoiceByIdQuery.Handler getInvoiceById,
        AddInvoicePaymentCommand.Handler addInvoicePayment,
        GetInvoicePdfQuery.Handler getInvoicePdf,
        UpdateInvoiceCommand.Handler updateInvoice,
        RefundInvoiceCommand.Handler refundInvoice,
        GetPatientInvoicesQuery.Handler getPatientInvoices,
        GetInvoiceDebtTrackingQuery.Handler debtTracking,
        GetInvoiceMonthlyRevenueQuery.Handler monthlyRevenue,
        GetInvoiceDailyRevenueReportQuery.Handler dailyRevenue,
        GetReceiptPdfQuery.Handler receiptPdf)
    {
        _currentUser = currentUser;
        _auditService = auditService;
        _createInvoice = createInvoice;
        _getInvoiceById = getInvoiceById;
        _addInvoicePayment = addInvoicePayment;
        _getInvoicePdf = getInvoicePdf;
        _updateInvoice = updateInvoice;
        _refundInvoice = refundInvoice;
        _getPatientInvoices = getPatientInvoices;
        _debtTracking = debtTracking;
        _monthlyRevenue = monthlyRevenue;
        _dailyRevenue = dailyRevenue;
        _receiptPdf = receiptPdf;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _createInvoice.Handle(new CreateInvoiceCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            UserId = _currentUser.UserId,
            Invoice = dto
        });

        if (result.Success)
        {
            await this.AuditAsync(_auditService, _currentUser, "Create", "Invoice", result.Data?.Id, new { result.Data?.Id, dto.PatientId });
        }

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _getInvoiceById.Handle(new GetInvoiceByIdQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            InvoiceId = id
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{id:guid}/payments")]
    public async Task<IActionResult> AddPayment(Guid id, [FromBody] AddInvoicePaymentDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _addInvoicePayment.Handle(new AddInvoicePaymentCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            InvoiceId = id,
            UserId = _currentUser.UserId,
            Payment = dto
        });

        if (result.Success)
        {
            await this.AuditAsync(_auditService, _currentUser, "AddPayment", "Invoice", id, new { id, dto.Amount });
        }

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> Pdf(Guid id)
    {
        if (!_currentUser.TenantId.HasValue) return Unauthorized();
        var result = await _getInvoicePdf.Handle(new(_currentUser.TenantId.Value, id));
        if (!result.Success || result.Data is null) return StatusCode(result.StatusCode, result);
        await this.AuditAsync(_auditService, _currentUser, "AccessPdf", "Invoice", id, new { id });
        return File(result.Data.Content, "application/pdf", result.Data.FileName);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInvoiceDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _updateInvoice.Handle(new UpdateInvoiceCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            InvoiceId = id,
            Invoice = dto
        });

        if (result.Success)
        {
            await this.AuditAsync(_auditService, _currentUser, "Update", "Invoice", id, new { id });
        }

        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{id:guid}/refund")]
    public async Task<IActionResult> Refund(Guid id, [FromBody] RefundInvoiceDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _refundInvoice.Handle(new RefundInvoiceCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            InvoiceId = id,
            Refund = dto
        });

        if (result.Success)
        {
            await this.AuditAsync(_auditService, _currentUser, "Refund", "Invoice", id, new { id });
        }

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("patient/{patientId:guid}")]
    public async Task<IActionResult> GetPatientInvoices(Guid patientId)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _getPatientInvoices.Handle(new GetPatientInvoicesQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            PatientId = patientId
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id:guid}/receipt")]
    public async Task<IActionResult> ReceiptPdf(Guid id)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _receiptPdf.Handle(new GetReceiptPdfQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            InvoiceId = id
        });

        if (!result.Success || result.Data is null)
        {
            return StatusCode(result.StatusCode, result);
        }

        await this.AuditAsync(_auditService, _currentUser, "AccessReceipt", "Invoice", id, new { id });

        return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
    }

    [HttpGet("debt-tracking")]
    public async Task<IActionResult> DebtTracking()
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _debtTracking.Handle(new GetInvoiceDebtTrackingQuery.Query
        {
            TenantId = _currentUser.TenantId.Value
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("monthly-revenue")]
    public async Task<IActionResult> MonthlyRevenue([FromQuery] int year, [FromQuery] int month)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _monthlyRevenue.Handle(new GetInvoiceMonthlyRevenueQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            Year = year,
            Month = month
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("daily-revenue")]
    public async Task<IActionResult> DailyRevenue([FromQuery] DateTime date)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _dailyRevenue.Handle(new GetInvoiceDailyRevenueReportQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            Date = date
        });

        return StatusCode(result.StatusCode, result);
    }
}
