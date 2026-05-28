using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Infrastructure.Data;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.UseCases.Appointments.Commands;
using Clinic.Saas.Service.UseCases.Appointments.Queries;
using Clinic.Saas.Service.UseCases.DrugCatalog.Queries;
using Clinic.Saas.Service.UseCases.Patients.Queries;
using Clinic.Saas.Service.UseCases.Payments.Commands;
using Clinic.Saas.Service.UseCases.Payments.Queries;
using Clinic.Saas.Service.UseCases.Prescriptions.Commands;
using Clinic.Saas.Service.UseCases.Prescriptions.Queries;
using Clinic.Saas.Service.UseCases.Users.Commands;
using Clinic.Saas.Service.UseCases.Users.Queries;
using Clinic.Saas.Service.UseCases.Visits.Commands;
using Clinic.Saas.Service.UseCases.Visits.Queries;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace Clinic.Saas.api.Controllers;

[Route("api/operations")]
[ApiController]
[Authorize]
public class OperationsController : ControllerBase
{
    private readonly DapperContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly GetAppointmentRangeQuery.Handler _getAppointmentRange;
    private readonly GetAppointmentCancellationsQuery.Handler _getAppointmentCancellations;
    private readonly RescheduleAppointmentCommand.Handler _rescheduleAppointment;
    private readonly GetPaymentByIdQuery.Handler _getPaymentById;
    private readonly GetPatientPaymentsQuery.Handler _getPatientPayments;
    private readonly UpdatePaymentCommand.Handler _updatePayment;
    private readonly RefundPaymentCommand.Handler _refundPayment;
    private readonly GetReceiptPdfQuery.Handler _receiptPdf;
    private readonly GetDebtTrackingQuery.Handler _debtTracking;
    private readonly GetMonthlyRevenueQuery.Handler _monthlyRevenue;
    private readonly GetPatientTimelineQuery.Handler _getPatientTimeline;
    private readonly FindPatientDuplicatesQuery.Handler _findPatientDuplicates;
    private readonly ExportPatientsQuery.Handler _exportPatients;
    private readonly GetPatientVisitsQuery.Handler _getPatientVisits;
    private readonly UpdateVisitCommand.Handler _updateVisit;
    private readonly FinalizeVisitCommand.Handler _finalizeVisit;
    private readonly GetPrescriptionPdfQuery.Handler _getPrescriptionPdf;
    private readonly SendPrescriptionWhatsappCommand.Handler _sendPrescriptionWhatsapp;
    private readonly SearchDrugsQuery.Handler _searchDrugs;
    private readonly CheckDrugInteractionsQuery.Handler _checkDrugInteractions;
    private readonly UpdateUserCommand.Handler _updateUser;
    private readonly DeactivateUserCommand.Handler _deactivateUser;
    private readonly ResetUserPasswordCommand.Handler _resetUserPassword;
    private readonly GetUserPreferencesQuery.Handler _getUserPreferences;
    private readonly SaveUserPreferencesCommand.Handler _saveUserPreferences;

    public OperationsController(
        DapperContext db,
        ICurrentUserService currentUser,
        GetAppointmentRangeQuery.Handler getAppointmentRange,
        GetAppointmentCancellationsQuery.Handler getAppointmentCancellations,
        RescheduleAppointmentCommand.Handler rescheduleAppointment,
        GetPaymentByIdQuery.Handler getPaymentById,
        GetPatientPaymentsQuery.Handler getPatientPayments,
        UpdatePaymentCommand.Handler updatePayment,
        RefundPaymentCommand.Handler refundPayment,
        GetReceiptPdfQuery.Handler receiptPdf,
        GetDebtTrackingQuery.Handler debtTracking,
        GetMonthlyRevenueQuery.Handler monthlyRevenue,
        GetPatientTimelineQuery.Handler getPatientTimeline,
        FindPatientDuplicatesQuery.Handler findPatientDuplicates,
        ExportPatientsQuery.Handler exportPatients,
        GetPatientVisitsQuery.Handler getPatientVisits,
        UpdateVisitCommand.Handler updateVisit,
        FinalizeVisitCommand.Handler finalizeVisit,
        GetPrescriptionPdfQuery.Handler getPrescriptionPdf,
        SendPrescriptionWhatsappCommand.Handler sendPrescriptionWhatsapp,
        SearchDrugsQuery.Handler searchDrugs,
        CheckDrugInteractionsQuery.Handler checkDrugInteractions,
        UpdateUserCommand.Handler updateUser,
        DeactivateUserCommand.Handler deactivateUser,
        ResetUserPasswordCommand.Handler resetUserPassword,
        GetUserPreferencesQuery.Handler getUserPreferences,
        SaveUserPreferencesCommand.Handler saveUserPreferences)
    {
        _db = db;
        _currentUser = currentUser;
        _getAppointmentRange = getAppointmentRange;
        _getAppointmentCancellations = getAppointmentCancellations;
        _rescheduleAppointment = rescheduleAppointment;
        _getPaymentById = getPaymentById;
        _getPatientPayments = getPatientPayments;
        _updatePayment = updatePayment;
        _refundPayment = refundPayment;
        _receiptPdf = receiptPdf;
        _debtTracking = debtTracking;
        _monthlyRevenue = monthlyRevenue;
        _getPatientTimeline = getPatientTimeline;
        _findPatientDuplicates = findPatientDuplicates;
        _exportPatients = exportPatients;
        _getPatientVisits = getPatientVisits;
        _updateVisit = updateVisit;
        _finalizeVisit = finalizeVisit;
        _getPrescriptionPdf = getPrescriptionPdf;
        _sendPrescriptionWhatsapp = sendPrescriptionWhatsapp;
        _searchDrugs = searchDrugs;
        _checkDrugInteractions = checkDrugInteractions;
        _updateUser = updateUser;
        _deactivateUser = deactivateUser;
        _resetUserPassword = resetUserPassword;
        _getUserPreferences = getUserPreferences;
        _saveUserPreferences = saveUserPreferences;
    }

   

    [Authorize(Roles = "Admin")]
    [HttpPut("users/{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
    {
        // Compatibility forwarding route. New canonical endpoint: PUT /api/users/{id}.
        if (!_currentUser.TenantId.HasValue) return Unauthorized();

        var result = await _updateUser.Handle(new UpdateUserCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            UserId = id,
            User = dto
        });

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("users/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateUser(Guid id)
    {
        // Compatibility forwarding route. New canonical endpoint: POST /api/users/{id}/deactivate.
        if (!_currentUser.TenantId.HasValue) return Unauthorized();

        var result = await _deactivateUser.Handle(new DeactivateUserCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            UserId = id
        });

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("users/{id:guid}/reset-password")]
    public async Task<IActionResult> ResetUserPassword(Guid id, [FromBody] ResetPasswordDto dto)
    {
        // Compatibility forwarding route. New canonical endpoint: POST /api/users/{id}/reset-password.
        if (!_currentUser.TenantId.HasValue) return Unauthorized();

        var result = await _resetUserPassword.Handle(new ResetUserPasswordCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            UserId = id,
            Password = dto
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("users/me/preferences")]
    public async Task<IActionResult> GetPreferences()
    {
        // Compatibility forwarding route. New canonical endpoint: GET /api/users/me/preferences.
        if (!_currentUser.TenantId.HasValue || !_currentUser.UserId.HasValue) return Unauthorized();

        var result = await _getUserPreferences.Handle(new GetUserPreferencesQuery.Query
        {
            TenantId = _currentUser.TenantId.Value,
            UserId = _currentUser.UserId.Value
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("users/me/preferences")]
    public async Task<IActionResult> SavePreferences([FromBody] UserPreferencesDto dto)
    {
        // Compatibility forwarding route. New canonical endpoint: PUT /api/users/me/preferences.
        if (!_currentUser.TenantId.HasValue || !_currentUser.UserId.HasValue) return Unauthorized();

        var result = await _saveUserPreferences.Handle(new SaveUserPreferencesCommand.Command
        {
            TenantId = _currentUser.TenantId.Value,
            UserId = _currentUser.UserId.Value,
            Preferences = dto
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("tenant/status")]
    public async Task<IActionResult> TenantStatus()
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();

        using var connection = _db.CreateConnection();
        var status = await connection.QueryFirstOrDefaultAsync<TenantSubscriptionStatusDto>(@"
SELECT
    COALESCE(t.SubscriptionState, N'Trial') AS State,
    t.TrialEndsAt,
    (SELECT TOP 1 EndDate FROM dbo.Subscriptions WHERE TenantId = t.Id AND Status IN (1,4) ORDER BY EndDate DESC) AS SubscriptionEndsAt,
    COALESCE(t.MaxUsers, 2) AS MaxUsers,
    COALESCE(t.MaxPatientsPerMonth, 200) AS MaxPatientsPerMonth
FROM dbo.Tenants t
WHERE t.Id = @TenantId;",
            new { TenantId = tenantId.Value });

        return OkResponse(status);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("tenant/settings")]
    public async Task<IActionResult> GetSettings()
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();
        using var connection = _db.CreateConnection();
        var settings = await EnsureSettings(connection, tenantId.Value);
        return OkResponse(settings);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("tenant/settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateClinicSettingsDto dto)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();
        using var connection = _db.CreateConnection();
        await connection.ExecuteAsync(@"
MERGE dbo.ClinicSettings AS target
USING (SELECT @TenantId AS TenantId) AS source
ON target.TenantId = source.TenantId
WHEN MATCHED THEN UPDATE SET
    WorkingDays = @WorkingDays,
    OpenTime = @OpenTime,
    CloseTime = @CloseTime,
    SlotDurationMin = @SlotDurationMin,
    ConsultFee = @ConsultFee,
    SmsEnabled = @SmsEnabled,
    WhatsappEnabled = @WhatsappEnabled,
    EmailEnabled = @EmailEnabled,
    [Language] = @Language,
    TaxPct = @TaxPct,
    UpdatedAt = SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT
    (Id, TenantId, WorkingDays, OpenTime, CloseTime, SlotDurationMin, ConsultFee, SmsEnabled, WhatsappEnabled, EmailEnabled, [Language], TaxPct, UpdatedAt)
VALUES
    (NEWID(), @TenantId, @WorkingDays, @OpenTime, @CloseTime, @SlotDurationMin, @ConsultFee, @SmsEnabled, @WhatsappEnabled, @EmailEnabled, @Language, @TaxPct, SYSUTCDATETIME());",
            new { TenantId = tenantId.Value, dto.WorkingDays, dto.OpenTime, dto.CloseTime, dto.SlotDurationMin, dto.ConsultFee, dto.SmsEnabled, dto.WhatsappEnabled, dto.EmailEnabled, dto.Language, dto.TaxPct });

        await Audit("Update", "ClinicSettings", tenantId.Value, dto);
        return await GetSettings();
    }

    [HttpGet("patients/{id:guid}/timeline")]
    public async Task<IActionResult> PatientTimeline(Guid id)
    {
        // Compatibility route. New code should call GET /api/patients/{id}/timeline.
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();

        var result = await _getPatientTimeline.Handle(new GetPatientTimelineQuery.Query
        {
            TenantId = tenantId.Value,
            PatientId = id
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("patients/duplicates")]
    public async Task<IActionResult> PatientDuplicates([FromQuery] string? phone, [FromQuery] string? nationalId)
    {
        // Compatibility route. New code should call GET /api/patients/duplicates.
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();

        var result = await _findPatientDuplicates.Handle(new FindPatientDuplicatesQuery.Query
        {
            TenantId = tenantId.Value,
            Phone = phone,
            NationalId = nationalId
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("patients/export")]
    public async Task<IActionResult> ExportPatients()
    {
        // Compatibility route. New code should call GET /api/patients/export.
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();

        var result = await _exportPatients.Handle(new ExportPatientsQuery.Query
        {
            TenantId = tenantId.Value
        });

        if (!result.Success || result.Data is null)
        {
            return StatusCode(result.StatusCode, result);
        }

        return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
    }

    

    [HttpGet("appointments/weekly")]
    public Task<IActionResult> WeeklyAppointments([FromQuery] DateTime weekStart)
    {
        // Compatibility route. New code should call GET /api/appointments/weekly.
        return AppointmentRange(weekStart.Date, weekStart.Date.AddDays(7));
    }

    [HttpGet("appointments/monthly")]
    public Task<IActionResult> MonthlyAppointments([FromQuery] int year, [FromQuery] int month)
    {
        // Compatibility route. New code should call GET /api/appointments/monthly.
        var start = new DateTime(year, month, 1);
        return AppointmentRange(start, start.AddMonths(1));
    }

    [HttpPut("appointments/{id:guid}/reschedule")]
    public async Task<IActionResult> RescheduleAppointment(Guid id, [FromBody] RescheduleAppointmentDto dto)
    {
        // Compatibility route. New code should call PUT /api/appointments/{id}/reschedule.
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();

        var result = await _rescheduleAppointment.Handle(new RescheduleAppointmentCommand.Command
        {
            TenantId = tenantId.Value,
            AppointmentId = id,
            Request = dto
        });

        if (result.Success)
        {
            await Audit("Reschedule", "Appointment", id, dto);
        }

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("appointments/cancellations")]
    public async Task<IActionResult> CancellationReport([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        // Compatibility route. New code should call GET /api/appointments/cancellations.
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();

        var result = await _getAppointmentCancellations.Handle(new GetAppointmentCancellationsQuery.Query
        {
            TenantId = tenantId.Value,
            From = from.Date,
            To = to.Date
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("online-bookings")]
    public async Task<IActionResult> OnlineBookings()
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();
        using var connection = _db.CreateConnection();
        var rows = await connection.QueryAsync(@"
SELECT Id, PatientName, PatientPhone, PatientEmail, RequestedDate, RequestedTime, DoctorId, Complaint, Status, ConfirmCode, RejectReason, CreatedAt
FROM dbo.OnlineBookings
WHERE TenantId = @TenantId
ORDER BY CreatedAt DESC;",
            new { TenantId = tenantId.Value });
        return OkResponse(rows);
    }

    [HttpPost("online-bookings/{id:guid}/approve")]
    public async Task<IActionResult> ApproveOnlineBooking(Guid id)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();
        using var connection = _db.CreateConnection();
        var rows = await connection.ExecuteAsync(
            "UPDATE dbo.OnlineBookings SET Status = @Status WHERE TenantId = @TenantId AND Id = @Id",
            new { TenantId = tenantId.Value, Id = id, Status = OnlineBookingStatus.Confirmed });
        if (rows == 0) return Error("Online booking not found.", StatusCodes.Status404NotFound);
        await Audit("Approve", "OnlineBooking", id, new { id });
        return OkResponse(true, "Online booking approved.");
    }

    [HttpPost("online-bookings/{id:guid}/reject")]
    public async Task<IActionResult> RejectOnlineBooking(Guid id, [FromBody] RejectOnlineBookingDto dto)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();
        using var connection = _db.CreateConnection();
        var rows = await connection.ExecuteAsync(
            "UPDATE dbo.OnlineBookings SET Status = @Status, RejectReason = @RejectReason WHERE TenantId = @TenantId AND Id = @Id",
            new { TenantId = tenantId.Value, Id = id, Status = OnlineBookingStatus.Rejected, dto.RejectReason });
        if (rows == 0) return Error("Online booking not found.", StatusCodes.Status404NotFound);
        await Audit("Reject", "OnlineBooking", id, dto);
        return OkResponse(true, "Online booking rejected.");
    }

    [HttpGet("visits/patient/{patientId:guid}")]
    public async Task<IActionResult> VisitHistory(Guid patientId)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();

        // Compatibility route: visit history logic lives in VisitsController/GetPatientVisitsQuery.
        var result = await _getPatientVisits.Handle(new GetPatientVisitsQuery.Query
        {
            TenantId = tenantId.Value,
            PatientId = patientId
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("visits/{id:guid}")]
    public async Task<IActionResult> UpdateVisit(Guid id, [FromBody] UpdateVisitDto dto)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();

        // Compatibility route: update logic lives in VisitsController/UpdateVisitCommand.
        var result = await _updateVisit.Handle(new UpdateVisitCommand.Command
        {
            TenantId = tenantId.Value,
            VisitId = id,
            Visit = dto
        });

        if (result.Success)
        {
            await Audit("Update", "Visit", id, dto);
        }

        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("visits/{id:guid}/finalize")]
    public async Task<IActionResult> FinalizeVisit(Guid id)
    {
        var tenantId = RequireTenant();
        if (tenantId is null || !_currentUser.UserId.HasValue) return Unauthorized();

        // Compatibility route: finalize logic lives in VisitsController/FinalizeVisitCommand.
        var result = await _finalizeVisit.Handle(new FinalizeVisitCommand.Command
        {
            TenantId = tenantId.Value,
            VisitId = id,
            FinalizedByUserId = _currentUser.UserId.Value
        });

        if (result.Success)
        {
            await Audit("Finalize", "Visit", id, new { id });
        }

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("clinical-templates")]
    public async Task<IActionResult> ClinicalTemplates()
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();
        using var connection = _db.CreateConnection();
        var rows = await connection.QueryAsync<ClinicalTemplateDto>("SELECT Id, Name, Specialty, ChiefComplaint, ClinicalNotes, Diagnosis FROM dbo.ClinicalTemplates WHERE TenantId = @TenantId AND IsActive = 1 ORDER BY Name", new { TenantId = tenantId.Value });
        return OkResponse(rows);
    }

    [HttpPost("clinical-templates")]
    public async Task<IActionResult> CreateClinicalTemplate([FromBody] CreateClinicalTemplateDto dto)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();
        var id = Guid.NewGuid();
        using var connection = _db.CreateConnection();
        await connection.ExecuteAsync(@"
INSERT INTO dbo.ClinicalTemplates (Id, TenantId, Name, Specialty, ChiefComplaint, ClinicalNotes, Diagnosis, IsActive, CreatedAt, UpdatedAt)
VALUES (@Id, @TenantId, @Name, @Specialty, @ChiefComplaint, @ClinicalNotes, @Diagnosis, 1, SYSUTCDATETIME(), SYSUTCDATETIME());",
            new { Id = id, TenantId = tenantId.Value, dto.Name, dto.Specialty, dto.ChiefComplaint, dto.ClinicalNotes, dto.Diagnosis });
        await Audit("Create", "ClinicalTemplate", id, dto);
        return OkResponse(new { id });
    }

    [HttpGet("prescriptions/{id:guid}/pdf")]
    public async Task<IActionResult> PrescriptionPdf(Guid id)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();

        // Compatibility route: prescription PDF logic lives in PrescriptionsController/GetPrescriptionPdfQuery.
        var result = await _getPrescriptionPdf.Handle(new GetPrescriptionPdfQuery.Query
        {
            TenantId = tenantId.Value,
            PrescriptionId = id
        });

        if (!result.Success || result.Data is null)
        {
            return StatusCode(result.StatusCode, result);
        }

        return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
    }

    [HttpPost("prescriptions/{id:guid}/send-whatsapp")]
    public async Task<IActionResult> SendPrescriptionWhatsapp(Guid id)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();

        // Compatibility route: send-whatsapp logic lives in PrescriptionsController/SendPrescriptionWhatsappCommand.
        var result = await _sendPrescriptionWhatsapp.Handle(new SendPrescriptionWhatsappCommand.Command
        {
            TenantId = tenantId.Value,
            PrescriptionId = id
        });

        if (result.Success)
        {
            await Audit("SendWhatsapp", "Prescription", id, new { id });
        }

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("drugs")]
    public async Task<IActionResult> DrugAutocomplete([FromQuery] string term = "")
    {
        // Compatibility route: Drugs is a global catalog and search logic lives in DrugCatalogController/SearchDrugsQuery.
        var result = await _searchDrugs.Handle(new SearchDrugsQuery.Query
        {
            Term = term
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("prescriptions/check-interactions")]
    public async Task<IActionResult> CheckDrugInteractions([FromBody] string[] drugNames)
    {
        // Compatibility route: Drugs is a global catalog and interaction logic lives in DrugCatalogController/CheckDrugInteractionsQuery.
        var result = await _checkDrugInteractions.Handle(new CheckDrugInteractionsQuery.Query
        {
            DrugNames = drugNames
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("billing/payments/{id:guid}")]
    public async Task<IActionResult> PaymentById(Guid id)
    {
        // Compatibility route. New code should call GET /api/billing/payments/{id}.
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();

        var result = await _getPaymentById.Handle(new GetPaymentByIdQuery.Query
        {
            TenantId = tenantId.Value,
            PaymentId = id
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("billing/patients/{patientId:guid}/payments")]
    public async Task<IActionResult> PatientPayments(Guid patientId)
    {
        // Compatibility route. New code should call GET /api/billing/patients/{patientId}/payments.
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();

        var result = await _getPatientPayments.Handle(new GetPatientPaymentsQuery.Query
        {
            TenantId = tenantId.Value,
            PatientId = patientId
        });

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "Admin,Reception")]
    [HttpPut("billing/payments/{id:guid}")]
    public async Task<IActionResult> UpdatePayment(Guid id, [FromBody] UpdatePaymentDto dto)
    {
        // Compatibility route. New code should call PUT /api/billing/payments/{id}.
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();

        var result = await _updatePayment.Handle(new UpdatePaymentCommand.Command
        {
            TenantId = tenantId.Value,
            PaymentId = id,
            Payment = dto
        });

        if (result.Success)
        {
            await Audit("Update", "Payment", id, dto);
        }

        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("billing/payments/{id:guid}/refund")]
    public async Task<IActionResult> RefundPayment(Guid id, [FromBody] RefundPaymentDto dto)
    {
        // Compatibility route. New code should call POST /api/billing/payments/{id}/refund.
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();

        var result = await _refundPayment.Handle(new RefundPaymentCommand.Command
        {
            TenantId = tenantId.Value,
            PaymentId = id,
            Refund = dto
        });

        if (result.Success)
        {
            await Audit("Refund", "Payment", id, dto);
        }

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("billing/payments/{id:guid}/receipt")]
    public async Task<IActionResult> ReceiptPdf(Guid id)
    {
        // Compatibility route. New code should call GET /api/billing/payments/{id}/receipt.
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();

        var result = await _receiptPdf.Handle(new GetReceiptPdfQuery.Query
        {
            TenantId = tenantId.Value,
            PaymentId = id
        });

        if (!result.Success || result.Data is null)
        {
            return StatusCode(result.StatusCode, result);
        }

        return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
    }

    [HttpGet("billing/debts")]
    public async Task<IActionResult> DebtTracking()
    {
        // Compatibility route. New code should call GET /api/billing/debts.
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();

        var result = await _debtTracking.Handle(new GetDebtTrackingQuery.Query
        {
            TenantId = tenantId.Value
        });

        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("billing/reports/monthly-revenue")]
    public async Task<IActionResult> MonthlyRevenue([FromQuery] int year, [FromQuery] int month)
    {
        // Compatibility route. New code should call GET /api/billing/reports/monthly-revenue.
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();

        var result = await _monthlyRevenue.Handle(new GetMonthlyRevenueQuery.Query
        {
            TenantId = tenantId.Value,
            Year = year,
            Month = month
        });

        return StatusCode(result.StatusCode, result);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("admin/usage")]
    public async Task<IActionResult> ClinicUsageMetrics()
    {
        using var connection = _db.CreateConnection();
        var rows = await connection.QueryAsync(@"
SELECT t.Id, t.Name, t.Subdomain,
       (SELECT COUNT(1) FROM dbo.Users u WHERE u.TenantId = t.Id AND u.IsActive = 1) AS UsersCount,
       (SELECT COUNT(1) FROM dbo.Patients p WHERE p.TenantId = t.Id AND p.IsDeleted = 0) AS PatientsCount,
       (SELECT COUNT(1) FROM dbo.Appointments a WHERE a.TenantId = t.Id AND a.IsDeleted = 0) AS AppointmentsCount
FROM dbo.Tenants t
ORDER BY t.CreatedAt DESC;");
        return OkResponse(rows);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("admin/subscription-revenue")]
    public async Task<IActionResult> SubscriptionRevenue()
    {
        using var connection = _db.CreateConnection();
        var rows = await connection.QueryAsync(@"
SELECT YEAR(CreatedAt) AS [Year], MONTH(CreatedAt) AS [Month], SUM(AmountPaid) AS Revenue, COUNT(1) AS SubscriptionCount
FROM dbo.Subscriptions
GROUP BY YEAR(CreatedAt), MONTH(CreatedAt)
ORDER BY [Year] DESC, [Month] DESC;");
        return OkResponse(rows);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("admin/expiring-subscriptions")]
    public async Task<IActionResult> ExpiringSubscriptions([FromQuery] int days = 14)
    {
        using var connection = _db.CreateConnection();
        var rows = await connection.QueryAsync(@"
SELECT t.Name, t.Subdomain, s.Plan, s.EndDate, s.Status
FROM dbo.Subscriptions s
INNER JOIN dbo.Tenants t ON t.Id = s.TenantId
WHERE s.EndDate >= SYSUTCDATETIME() AND s.EndDate < DATEADD(day, @Days, SYSUTCDATETIME())
ORDER BY s.EndDate;",
            new { Days = days });
        return OkResponse(rows);
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet("admin/activity-log")]
    public async Task<IActionResult> ActivityLog([FromQuery] int take = 100)
    {
        using var connection = _db.CreateConnection();
        var rows = await connection.QueryAsync<AuditLogDto>(@"
SELECT TOP (@Take) Id, TenantId, UserId, Action, EntityName, EntityId, NewValues, CreatedAt
FROM dbo.AuditLogs
WHERE (@TenantId IS NULL OR TenantId = @TenantId)
ORDER BY CreatedAt DESC;",
            new { Take = Math.Clamp(take, 1, 500), TenantId = _currentUser.Role == UserRole.SuperAdmin ? null : _currentUser.TenantId });
        return OkResponse(rows);
    }

    private async Task<IActionResult> AppointmentRange(DateTime from, DateTime to)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();

        var result = await _getAppointmentRange.Handle(new GetAppointmentRangeQuery.Query
        {
            TenantId = tenantId.Value,
            From = from.Date,
            To = to.Date,
            DoctorId = _currentUser.Role == UserRole.Doctor ? _currentUser.UserId : null
        });

        return StatusCode(result.StatusCode, result);
    }

    private Guid? RequireTenant() => _currentUser.TenantId;

    private async Task<UpdateClinicSettingsDto> EnsureSettings(System.Data.IDbConnection connection, Guid tenantId)
    {
        var settings = await connection.QueryFirstOrDefaultAsync<UpdateClinicSettingsDto>(
            "SELECT WorkingDays, OpenTime, CloseTime, SlotDurationMin, ConsultFee, SmsEnabled, WhatsappEnabled, EmailEnabled, [Language], TaxPct FROM dbo.ClinicSettings WHERE TenantId = @TenantId",
            new { TenantId = tenantId });
        if (settings is not null) return settings;
        return new UpdateClinicSettingsDto();
    }

    private async Task Audit(string action, string entityName, Guid? entityId, object? newValues)
    {
        try
        {
            using var connection = _db.CreateConnection();
            await connection.ExecuteAsync(@"
INSERT INTO dbo.AuditLogs (TenantId, UserId, Action, EntityName, EntityId, NewValues, IpAddress, UserAgent, CreatedAt)
VALUES (@TenantId, @UserId, @Action, @EntityName, @EntityId, @NewValues, @IpAddress, @UserAgent, SYSUTCDATETIME());",
                new
                {
                    TenantId = _currentUser.TenantId,
                    UserId = _currentUser.UserId,
                    Action = action,
                    EntityName = entityName,
                    EntityId = entityId,
                    NewValues = newValues is null ? null : JsonSerializer.Serialize(newValues),
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers.UserAgent.ToString()
                });
        }
        catch
        {
            // Audit logging must not break the user operation.
        }
    }

    private IActionResult OkResponse<T>(T data, string message = "OK")
    {
        return StatusCode(StatusCodes.Status200OK, new BaseResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            StatusCode = StatusCodes.Status200OK
        });
    }

    private IActionResult Error(string message, int statusCode)
    {
        return StatusCode(statusCode, new BaseResponse<object>
        {
            Success = false,
            Message = message,
            Errors = [message],
            StatusCode = statusCode
        });
    }

    private static string Csv(object? value)
    {
        var text = value?.ToString() ?? string.Empty;
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }

    private static byte[] CreateSimplePdf(string text)
    {
        var escaped = text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("\r", "").Replace("\n", ") Tj T* (");
        var stream = $"BT /F1 12 Tf 50 780 Td ({escaped}) Tj ET";
        var pdf = $@"%PDF-1.4
1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj
2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj
3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >> endobj
4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj
5 0 obj << /Length {stream.Length} >> stream
{stream}
endstream endobj
xref
0 6
0000000000 65535 f 
trailer << /Root 1 0 R /Size 6 >>
startxref
0
%%EOF";
        return Encoding.ASCII.GetBytes(pdf);
    }

}
