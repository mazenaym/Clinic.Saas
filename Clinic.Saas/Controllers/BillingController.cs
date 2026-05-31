using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.Security;
using Clinic.Saas.Service.UseCases.Payments.Commands;
using Clinic.Saas.Service.UseCases.Payments.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BillingController : ControllerBase
{
    private readonly CreatePaymentCommand.Handler _createPayment;
    private readonly GetPaymentByIdQuery.Handler _getPaymentById;
    private readonly GetPatientPaymentsQuery.Handler _getPatientPayments;
    private readonly UpdatePaymentCommand.Handler _updatePayment;
    private readonly RefundPaymentCommand.Handler _refundPayment;
    private readonly GetReceiptPdfQuery.Handler _receiptPdf;
    private readonly GetDebtTrackingQuery.Handler _debtTracking;
    private readonly GetMonthlyRevenueQuery.Handler _monthlyRevenue;
    private readonly GetDailyRevenueReportQuery.Handler _dailyRevenue;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public BillingController(
        CreatePaymentCommand.Handler createPayment,
        GetPaymentByIdQuery.Handler getPaymentById,
        GetPatientPaymentsQuery.Handler getPatientPayments,
        UpdatePaymentCommand.Handler updatePayment,
        RefundPaymentCommand.Handler refundPayment,
        GetReceiptPdfQuery.Handler receiptPdf,
        GetDebtTrackingQuery.Handler debtTracking,
        GetMonthlyRevenueQuery.Handler monthlyRevenue,
        GetDailyRevenueReportQuery.Handler dailyRevenue,
        ICurrentUserService currentUser,
        IAuditService auditService)
    {
        _createPayment = createPayment;
        _getPaymentById = getPaymentById;
        _getPatientPayments = getPatientPayments;
        _updatePayment = updatePayment;
        _refundPayment = refundPayment;
        _receiptPdf = receiptPdf;
        _debtTracking = debtTracking;
        _monthlyRevenue = monthlyRevenue;
        _dailyRevenue = dailyRevenue;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    [Authorize(Roles = "Admin,Reception", Policy = Permissions.BillingManage)]
    [HttpPost("payments")]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _createPayment.Handle(new CreatePaymentCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            Payment = dto
        });

        if (result.Success)
        {
            await this.AuditAsync(_auditService, _currentUser, "Create", "Payment", result.Data?.Id, new { result.Data?.Id });
        }

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin,Reception", Policy = Permissions.BillingView)]
    [HttpGet("payments/{id:guid}")]
    public async Task<IActionResult> GetPaymentById(Guid id)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _getPaymentById.Handle(new GetPaymentByIdQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            PaymentId = id
        });

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin,Reception", Policy = Permissions.BillingView)]
    [HttpGet("patients/{patientId:guid}/payments")]
    public async Task<IActionResult> GetPatientPayments(Guid patientId)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _getPatientPayments.Handle(new GetPatientPaymentsQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            PatientId = patientId
        });

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin,Reception", Policy = Permissions.BillingManage)]
    [HttpPut("payments/{id:guid}")]
    public async Task<IActionResult> UpdatePayment(Guid id, [FromBody] UpdatePaymentDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _updatePayment.Handle(new UpdatePaymentCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            PaymentId = id,
            Payment = dto
        });

        if (result.Success)
        {
            await this.AuditAsync(_auditService, _currentUser, "Update", "Payment", id, new { id });
        }

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin,Reception", Policy = Permissions.BillingManage)]
    [HttpPost("payments/{id:guid}/refund")]
    public async Task<IActionResult> RefundPayment(Guid id, [FromBody] RefundPaymentDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _refundPayment.Handle(new RefundPaymentCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            PaymentId = id,
            Refund = dto
        });

        if (result.Success)
        {
            await this.AuditAsync(_auditService, _currentUser, "Refund", "Payment", id, new { id });
        }

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin,Reception", Policy = Permissions.BillingView)]
    [HttpGet("payments/{id:guid}/receipt")]
    public async Task<IActionResult> ReceiptPdf(Guid id)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _receiptPdf.Handle(new GetReceiptPdfQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            PaymentId = id
        });

        if (!result.Success || result.Data is null)
        {
            return StatusCode(result.StatusCode, result);
        }

        await this.AuditAsync(_auditService, _currentUser, "AccessReceipt", "Payment", id, new { id });

        return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
    }

    [Authorize(Roles = "Admin,Reception", Policy = Permissions.BillingView)]
    [HttpGet("debts")]
    public async Task<IActionResult> DebtTracking()
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _debtTracking.Handle(new GetDebtTrackingQuery.Query
        {
            TenantId = _currentUser.TenantId.Value
        });

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin", Policy = Permissions.ReportsFinancialView)]
    [HttpGet("reports/monthly-revenue")]
    public async Task<IActionResult> MonthlyRevenue([FromQuery] int year, [FromQuery] int month)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _monthlyRevenue.Handle(new GetMonthlyRevenueQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            Year = year,
            Month = month
        });

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin", Policy = Permissions.ReportsFinancialView)]
    [HttpGet("reports/daily-revenue")]
    public async Task<IActionResult> DailyRevenue([FromQuery] DateTime date)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _dailyRevenue.Handle(new GetDailyRevenueReportQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            Date = date
        });

        return StatusCode(result.StatusCode, result);
    }
}
