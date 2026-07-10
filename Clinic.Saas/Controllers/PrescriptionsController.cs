using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.UseCases.Prescriptions.Commands;
using Clinic.Saas.Service.UseCases.Prescriptions.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers;

[Route("api/prescriptions")]
[ApiController]
[Authorize]
public class PrescriptionsController : ControllerBase
{
    private readonly CreatePrescriptionCommand.Handler _createPrescription;
    private readonly GetPrescriptionByIdQuery.Handler _getPrescription;
    private readonly GetPrescriptionPdfQuery.Handler _getPrescriptionPdf;
    private readonly SendPrescriptionWhatsappCommand.Handler _sendWhatsapp;
    private readonly ICurrentUserService _currentUser;
    private readonly IClinicAuthorizationService _authorization;
    private readonly IAuditService _auditService;

    public PrescriptionsController(
        CreatePrescriptionCommand.Handler createPrescription,
        GetPrescriptionByIdQuery.Handler getPrescription,
        GetPrescriptionPdfQuery.Handler getPrescriptionPdf,
        SendPrescriptionWhatsappCommand.Handler sendWhatsapp,
        ICurrentUserService currentUser,
        IClinicAuthorizationService authorization,
        IAuditService auditService)
    {
        _createPrescription = createPrescription;
        _getPrescription = getPrescription;
        _getPrescriptionPdf = getPrescriptionPdf;
        _sendWhatsapp = sendWhatsapp;
        _currentUser = currentUser;
        _authorization = authorization;
        _auditService = auditService;
    }

    [Authorize(Roles = "Admin,Doctor")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePrescriptionDto dto)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        if (_currentUser.Role == Domain.Enums.UserRole.Doctor && _currentUser.UserId.HasValue)
        {
            dto.DoctorId = _currentUser.UserId.Value;
        }

        var result = await _createPrescription.Handle(new CreatePrescriptionCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            Prescription = dto
        });

        if (result.Success)
        {
            await this.AuditAsync(_auditService, _currentUser, "Create", "Prescription", result.Data?.Id, new { result.Data?.Id });
        }

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin,Doctor")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        if (!await _authorization.CanViewPrescriptionAsync(_currentUser.TenantId.Value, id))
        {
            return Forbid();
        }

        var result = await _getPrescription.Handle(new GetPrescriptionByIdQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            Id = id
        });
        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin,Doctor")]
    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> Pdf(Guid id)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _getPrescriptionPdf.Handle(new GetPrescriptionPdfQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            PrescriptionId = id
        });

        if (!result.Success || result.Data is null)
        {
            return StatusCode(result.StatusCode, result);
        }

        await this.AuditAsync(_auditService, _currentUser, "AccessPdf", "Prescription", id, new { id });

        return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
    }

    [Authorize(Roles = "Admin,Doctor")]
    [HttpPost("{id:guid}/send-whatsapp")]
    public async Task<IActionResult> SendWhatsapp(Guid id, [FromHeader(Name = "If-Match")] string? rowVersion)
    {
        if (!_currentUser.TenantId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _sendWhatsapp.Handle(new SendPrescriptionWhatsappCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            PrescriptionId = id,
            RowVersion = rowVersion
        });

        if (result.Success)
        {
            await this.AuditAsync(_auditService, _currentUser, "SendWhatsapp", "Prescription", id, new { id });
        }

        return StatusCode(result.StatusCode, result);
    }
}
