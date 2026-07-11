using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
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

    public InvoicesController(
        ICurrentUserService currentUser,
        IAuditService auditService,
        CreateInvoiceCommand.Handler createInvoice,
        GetInvoiceByIdQuery.Handler getInvoiceById,
        AddInvoicePaymentCommand.Handler addInvoicePayment,
        GetInvoicePdfQuery.Handler getInvoicePdf)
    {
        _currentUser = currentUser;
        _auditService = auditService;
        _createInvoice = createInvoice;
        _getInvoiceById = getInvoiceById;
        _addInvoicePayment = addInvoicePayment;
        _getInvoicePdf = getInvoicePdf;
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
}
