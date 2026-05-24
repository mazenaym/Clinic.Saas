using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Infrastructure.Data;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
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
    private readonly IPasswordService _passwordService;

    public OperationsController(DapperContext db, ICurrentUserService currentUser, IPasswordService passwordService)
    {
        _db = db;
        _currentUser = currentUser;
        _passwordService = passwordService;
    }

    [HttpPost("auth/change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Unauthorized();
        }

        using var connection = _db.CreateConnection();
        var user = await connection.QueryFirstOrDefaultAsync<UserPasswordRow>(
            "SELECT Id, PasswordHash FROM dbo.Users WHERE Id = @UserId AND IsActive = 1",
            new { UserId = _currentUser.UserId.Value });

        if (user is null || !_passwordService.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
        {
            return Error("Current password is incorrect.", StatusCodes.Status400BadRequest);
        }

        await connection.ExecuteAsync(
            "UPDATE dbo.Users SET PasswordHash = @Hash, RefreshToken = NULL, RefreshTokenExpiry = NULL, UpdatedAt = SYSUTCDATETIME() WHERE Id = @UserId",
            new { UserId = user.Id, Hash = _passwordService.HashPassword(dto.NewPassword) });

        await Audit("ChangePassword", "User", user.Id, new { user.Id });
        return OkResponse(true, "Password changed successfully.");
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("users/{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();

        using var connection = _db.CreateConnection();
        var existing = await connection.QueryFirstOrDefaultAsync<UserListRow>(
            "SELECT * FROM dbo.Users WHERE TenantId = @TenantId AND Id = @Id",
            new { TenantId = tenantId.Value, Id = id });

        if (existing is null)
        {
            return Error("User not found.", StatusCodes.Status404NotFound);
        }

        var emailTaken = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM dbo.Users WHERE TenantId = @TenantId AND Id <> @Id AND LOWER(Email) = LOWER(@Email)",
            new { TenantId = tenantId.Value, Id = id, dto.Email });
        if (emailTaken > 0)
        {
            return Error("Email is already used in this clinic.", StatusCodes.Status409Conflict);
        }

        await connection.ExecuteAsync(@"
UPDATE dbo.Users
SET FullName = @FullName,
    Email = @Email,
    Role = @Role,
    Phone = @Phone,
    Specialty = @Specialty,
    LicenseNumber = @LicenseNumber,
    UpdatedAt = SYSUTCDATETIME()
WHERE TenantId = @TenantId AND Id = @Id;",
            new
            {
                TenantId = tenantId.Value,
                Id = id,
                dto.FullName,
                dto.Email,
                Role = dto.Role,
                dto.Phone,
                dto.Specialty,
                dto.LicenseNumber
            });

        await Audit("Update", "User", id, dto);
        return await UserById(connection, tenantId.Value, id);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("users/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateUser(Guid id)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();

        using var connection = _db.CreateConnection();
        var user = await connection.QueryFirstOrDefaultAsync<UserListRow>(
            "SELECT * FROM dbo.Users WHERE TenantId = @TenantId AND Id = @Id",
            new { TenantId = tenantId.Value, Id = id });

        if (user is null)
        {
            return Error("User not found.", StatusCodes.Status404NotFound);
        }

        if (user.Role == UserRole.Admin)
        {
            var activeAdmins = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM dbo.Users WHERE TenantId = @TenantId AND Role = @AdminRole AND IsActive = 1",
                new { TenantId = tenantId.Value, AdminRole = UserRole.Admin });
            if (activeAdmins <= 1)
            {
                return Error("Cannot deactivate the last active admin in the clinic.", StatusCodes.Status409Conflict);
            }
        }

        await connection.ExecuteAsync(
            "UPDATE dbo.Users SET IsActive = 0, RefreshToken = NULL, RefreshTokenExpiry = NULL, UpdatedAt = SYSUTCDATETIME() WHERE TenantId = @TenantId AND Id = @Id",
            new { TenantId = tenantId.Value, Id = id });

        await Audit("Deactivate", "User", id, new { id });
        return OkResponse(true, "User deactivated.");
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("users/{id:guid}/reset-password")]
    public async Task<IActionResult> ResetUserPassword(Guid id, [FromBody] ResetPasswordDto dto)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();

        using var connection = _db.CreateConnection();
        var rows = await connection.ExecuteAsync(@"
UPDATE dbo.Users
SET PasswordHash = @Hash,
    RefreshToken = NULL,
    RefreshTokenExpiry = NULL,
    UpdatedAt = SYSUTCDATETIME()
WHERE TenantId = @TenantId AND Id = @Id;",
            new { TenantId = tenantId.Value, Id = id, Hash = _passwordService.HashPassword(dto.NewPassword) });

        if (rows == 0)
        {
            return Error("User not found.", StatusCodes.Status404NotFound);
        }

        await Audit("ResetPassword", "User", id, new { id });
        return OkResponse(true, "Password reset successfully.");
    }

    [HttpGet("users/me/preferences")]
    public IActionResult GetPreferences()
    {
        return OkResponse(new UserPreferencesDto { Language = "ar", Theme = "light" });
    }

    [HttpPut("users/me/preferences")]
    public async Task<IActionResult> SavePreferences([FromBody] UserPreferencesDto dto)
    {
        if (!_currentUser.UserId.HasValue) return Unauthorized();
        using var connection = _db.CreateConnection();
        await connection.ExecuteAsync(
            "UPDATE dbo.Users SET AvatarUrl = COALESCE(@AvatarUrl, AvatarUrl), UpdatedAt = SYSUTCDATETIME() WHERE Id = @UserId",
            new { UserId = _currentUser.UserId.Value, dto.AvatarUrl });
        await Audit("UpdatePreferences", "User", _currentUser.UserId.Value, dto);
        return OkResponse(dto);
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
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();
        using var connection = _db.CreateConnection();

        var rows = await connection.QueryAsync<PatientTimelineItemDto>(@"
SELECT 'Appointment' AS [Type], Id, CAST(AppointmentDate AS datetime2) AS [Date], CONCAT(N'Appointment ', CAST([Status] AS nvarchar(10))) AS Title, Notes AS Details
FROM dbo.Appointments WHERE TenantId = @TenantId AND PatientId = @PatientId AND IsDeleted = 0
UNION ALL
SELECT 'Visit', Id, VisitDate, ChiefComplaint, Diagnosis FROM dbo.Visits WHERE TenantId = @TenantId AND PatientId = @PatientId AND IsDeleted = 0
UNION ALL
SELECT 'Prescription', Id, CreatedAt, N'Prescription', Notes FROM dbo.Prescriptions WHERE TenantId = @TenantId AND PatientId = @PatientId AND IsActive = 1
UNION ALL
SELECT 'Payment', Id, CreatedAt, InvoiceNumber, CONCAT(N'Paid ', PaidAmount, N' / Total ', TotalAmount) FROM dbo.Payments WHERE TenantId = @TenantId AND PatientId = @PatientId
ORDER BY [Date] DESC;",
            new { TenantId = tenantId.Value, PatientId = id });

        return OkResponse(rows);
    }

    [HttpGet("patients/duplicates")]
    public async Task<IActionResult> PatientDuplicates([FromQuery] string? phone, [FromQuery] string? nationalId)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();
        using var connection = _db.CreateConnection();
        var rows = await connection.QueryAsync(@"
SELECT TOP 20 Id, PatientCode, FullName, PhoneNumber, NationalId
FROM dbo.Patients
WHERE TenantId = @TenantId AND IsDeleted = 0
  AND ((@Phone IS NOT NULL AND PhoneNumber = @Phone) OR (@NationalId IS NOT NULL AND NationalId = @NationalId));",
            new { TenantId = tenantId.Value, Phone = string.IsNullOrWhiteSpace(phone) ? null : phone, NationalId = string.IsNullOrWhiteSpace(nationalId) ? null : nationalId });
        return OkResponse(rows);
    }

    [HttpGet("patients/export")]
    public async Task<IActionResult> ExportPatients()
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();
        using var connection = _db.CreateConnection();
        var rows = await connection.QueryAsync(@"
SELECT PatientCode, FullName, PhoneNumber, NationalId, Email, Gender, CreatedAt
FROM dbo.Patients
WHERE TenantId = @TenantId AND IsDeleted = 0
ORDER BY CreatedAt DESC;",
            new { TenantId = tenantId.Value });

        var csv = new StringBuilder();
        csv.AppendLine("PatientCode,FullName,PhoneNumber,NationalId,Email,Gender,CreatedAt");
        foreach (var row in rows)
        {
            csv.AppendLine($"{Csv(row.PatientCode)},{Csv(row.FullName)},{Csv(row.PhoneNumber)},{Csv(row.NationalId)},{Csv(row.Email)},{row.Gender},{row.CreatedAt:O}");
        }

        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(), "text/csv", "patients-export.csv");
    }

    [HttpPost("patients/{patientId:guid}/documents")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadPatientDocument(Guid patientId, IFormFile file, [FromForm] short documentType = 1, [FromForm] string? description = null)
    {
        var tenantId = RequireTenant();
        if (tenantId is null || !_currentUser.UserId.HasValue) return Unauthorized();
        if (file.Length == 0) return Error("File is empty.", StatusCodes.Status400BadRequest);

        var uploads = Path.Combine(AppContext.BaseDirectory, "uploads", tenantId.Value.ToString(), "patients", patientId.ToString());
        Directory.CreateDirectory(uploads);
        var safeName = $"{Guid.NewGuid()}-{Path.GetFileName(file.FileName)}";
        var path = Path.Combine(uploads, safeName);
        await using (var stream = System.IO.File.Create(path))
        {
            await file.CopyToAsync(stream);
        }

        var id = Guid.NewGuid();
        using var connection = _db.CreateConnection();
        await connection.ExecuteAsync(@"
INSERT INTO dbo.PatientDocuments
(Id, TenantId, PatientId, FileName, FileUrl, FileSizeKb, FileType, DocumentType, Description, UploadedBy, UploadedAt)
VALUES
(@Id, @TenantId, @PatientId, @FileName, @FileUrl, @FileSizeKb, @FileType, @DocumentType, @Description, @UploadedBy, SYSUTCDATETIME());",
            new
            {
                Id = id,
                TenantId = tenantId.Value,
                PatientId = patientId,
                FileName = file.FileName,
                FileUrl = path,
                FileSizeKb = (int)Math.Ceiling(file.Length / 1024d),
                FileType = file.ContentType,
                DocumentType = documentType,
                Description = description,
                UploadedBy = _currentUser.UserId.Value
            });

        await Audit("Upload", "PatientDocument", id, new { patientId, file.FileName });
        return OkResponse(new { id, file.FileName, path });
    }

    [HttpGet("appointments/weekly")]
    public Task<IActionResult> WeeklyAppointments([FromQuery] DateTime weekStart) => AppointmentRange(weekStart.Date, weekStart.Date.AddDays(7));

    [HttpGet("appointments/monthly")]
    public Task<IActionResult> MonthlyAppointments([FromQuery] int year, [FromQuery] int month)
    {
        var start = new DateTime(year, month, 1);
        return AppointmentRange(start, start.AddMonths(1));
    }

    [HttpPut("appointments/{id:guid}/reschedule")]
    public async Task<IActionResult> RescheduleAppointment(Guid id, [FromBody] RescheduleAppointmentDto dto)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();
        using var connection = _db.CreateConnection();

        var appointment = await connection.QueryFirstOrDefaultAsync(
            "SELECT Id, DoctorId FROM dbo.Appointments WHERE TenantId = @TenantId AND Id = @Id AND IsDeleted = 0",
            new { TenantId = tenantId.Value, Id = id });
        if (appointment is null) return Error("Appointment not found.", StatusCodes.Status404NotFound);

        var conflict = await connection.ExecuteScalarAsync<int>(@"
SELECT COUNT(1) FROM dbo.Appointments
WHERE TenantId = @TenantId AND DoctorId = @DoctorId AND Id <> @Id AND AppointmentDate = @Date AND IsDeleted = 0 AND Status <> @Cancelled
  AND StartTime < @EndTime AND EndTime > @StartTime;",
            new { TenantId = tenantId.Value, DoctorId = appointment.DoctorId, Id = id, Date = dto.AppointmentDate.Date, dto.StartTime, dto.EndTime, Cancelled = AppointmentStatus.Cancelled });
        if (conflict > 0) return Error("Appointment conflicts with another booking.", StatusCodes.Status409Conflict);

        await connection.ExecuteAsync(@"
UPDATE dbo.Appointments
SET AppointmentDate = @Date, StartTime = @StartTime, EndTime = @EndTime, UpdatedAt = SYSUTCDATETIME()
WHERE TenantId = @TenantId AND Id = @Id;",
            new { TenantId = tenantId.Value, Id = id, Date = dto.AppointmentDate.Date, dto.StartTime, dto.EndTime });

        await Audit("Reschedule", "Appointment", id, dto);
        return OkResponse(true, "Appointment rescheduled.");
    }

    [HttpGet("appointments/cancellations")]
    public async Task<IActionResult> CancellationReport([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();
        using var connection = _db.CreateConnection();
        var rows = await connection.QueryAsync(@"
SELECT Id, AppointmentDate, StartTime, EndTime, CancelReason, UpdatedAt
FROM dbo.Appointments
WHERE TenantId = @TenantId AND Status = @Cancelled AND AppointmentDate >= @From AND AppointmentDate < @To
ORDER BY AppointmentDate DESC;",
            new { TenantId = tenantId.Value, Cancelled = AppointmentStatus.Cancelled, From = from.Date, To = to.Date.AddDays(1) });
        return OkResponse(rows);
    }

    [HttpGet("visits/patient/{patientId:guid}")]
    public async Task<IActionResult> VisitHistory(Guid patientId)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();
        using var connection = _db.CreateConnection();
        var rows = await connection.QueryAsync("SELECT * FROM dbo.Visits WHERE TenantId = @TenantId AND PatientId = @PatientId AND IsDeleted = 0 ORDER BY VisitDate DESC", new { TenantId = tenantId.Value, PatientId = patientId });
        return OkResponse(rows);
    }

    [HttpPut("visits/{id:guid}")]
    public async Task<IActionResult> UpdateVisit(Guid id, [FromBody] UpdateVisitDto dto)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();
        using var connection = _db.CreateConnection();
        var locked = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM dbo.Visits WHERE TenantId = @TenantId AND Id = @Id AND FinalizedAt IS NOT NULL", new { TenantId = tenantId.Value, Id = id });
        if (locked > 0) return Error("Visit is finalized and cannot be updated.", StatusCodes.Status409Conflict);

        var rows = await connection.ExecuteAsync(@"
UPDATE dbo.Visits
SET VisitType = @VisitType, ChiefComplaint = @ChiefComplaint, VitalSigns = @VitalSigns, ClinicalNotes = @ClinicalNotes,
    Diagnosis = @Diagnosis, DiagnosisCode = @DiagnosisCode, FollowUpDate = @FollowUpDate, UpdatedAt = SYSUTCDATETIME()
WHERE TenantId = @TenantId AND Id = @Id AND IsDeleted = 0;",
            new { TenantId = tenantId.Value, Id = id, dto.VisitType, dto.ChiefComplaint, VitalSigns = JsonSerializer.Serialize(dto.VitalSigns), dto.ClinicalNotes, dto.Diagnosis, dto.DiagnosisCode, dto.FollowUpDate });
        if (rows == 0) return Error("Visit not found.", StatusCodes.Status404NotFound);
        await Audit("Update", "Visit", id, dto);
        return OkResponse(true, "Visit updated.");
    }

    [HttpPost("visits/{id:guid}/finalize")]
    public async Task<IActionResult> FinalizeVisit(Guid id)
    {
        var tenantId = RequireTenant();
        if (tenantId is null || !_currentUser.UserId.HasValue) return Unauthorized();
        using var connection = _db.CreateConnection();
        var rows = await connection.ExecuteAsync("UPDATE dbo.Visits SET FinalizedAt = SYSUTCDATETIME(), FinalizedBy = @UserId, UpdatedAt = SYSUTCDATETIME() WHERE TenantId = @TenantId AND Id = @Id AND IsDeleted = 0", new { TenantId = tenantId.Value, Id = id, UserId = _currentUser.UserId.Value });
        if (rows == 0) return Error("Visit not found.", StatusCodes.Status404NotFound);
        await Audit("Finalize", "Visit", id, new { id });
        return OkResponse(true, "Visit finalized.");
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
        using var connection = _db.CreateConnection();
        var prescription = await connection.QueryFirstOrDefaultAsync(@"
SELECT pr.Id, p.FullName AS PatientName, u.FullName AS DoctorName, pr.Notes, pr.CreatedAt
FROM dbo.Prescriptions pr
INNER JOIN dbo.Patients p ON p.Id = pr.PatientId AND p.TenantId = pr.TenantId
INNER JOIN dbo.Users u ON u.Id = pr.DoctorId AND u.TenantId = pr.TenantId
WHERE pr.TenantId = @TenantId AND pr.Id = @Id;",
            new { TenantId = tenantId.Value, Id = id });
        if (prescription is null) return Error("Prescription not found.", StatusCodes.Status404NotFound);
        var items = await connection.QueryAsync("SELECT DrugName, Dosage, Frequency, Duration, Instructions FROM dbo.PrescriptionItems WHERE PrescriptionId = @Id ORDER BY SortOrder", new { Id = id });
        var body = $"Prescription\nPatient: {prescription.PatientName}\nDoctor: {prescription.DoctorName}\nDate: {prescription.CreatedAt:yyyy-MM-dd}\n\n" + string.Join("\n", items.Select(i => $"- {i.DrugName} {i.Dosage} {i.Frequency} {i.Duration} {i.Instructions}"));
        return File(CreateSimplePdf(body), "application/pdf", $"prescription-{id}.pdf");
    }

    [HttpPost("prescriptions/{id:guid}/send-whatsapp")]
    public async Task<IActionResult> SendPrescriptionWhatsapp(Guid id)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();
        using var connection = _db.CreateConnection();
        var settings = await EnsureSettings(connection, tenantId.Value);
        if (!settings.WhatsappEnabled)
        {
            return Error("WhatsApp integration is disabled for this clinic.", StatusCodes.Status409Conflict);
        }

        await connection.ExecuteAsync("UPDATE dbo.Prescriptions SET SentViaWhatsapp = 1 WHERE TenantId = @TenantId AND Id = @Id", new { TenantId = tenantId.Value, Id = id });
        await Audit("SendWhatsapp", "Prescription", id, new { id });
        return OkResponse(true, "Prescription marked as sent via WhatsApp.");
    }

    [HttpGet("drugs")]
    public async Task<IActionResult> DrugAutocomplete([FromQuery] string term = "")
    {
        using var connection = _db.CreateConnection();
        var rows = await connection.QueryAsync(@"
SELECT TOP 20 Id, TradeName, GenericName, Strength, Form, Interactions
FROM dbo.Drugs
WHERE IsActive = 1 AND (@Term = '' OR TradeName LIKE @Search OR GenericName LIKE @Search)
ORDER BY TradeName;",
            new { Term = term ?? "", Search = $"%{term}%" });
        return OkResponse(rows);
    }

    [HttpPost("prescriptions/check-interactions")]
    public async Task<IActionResult> CheckDrugInteractions([FromBody] string[] drugNames)
    {
        using var connection = _db.CreateConnection();
        var drugs = await connection.QueryAsync<(string TradeName, string? Interactions)>("SELECT TradeName, Interactions FROM dbo.Drugs WHERE IsActive = 1 AND TradeName IN @Names", new { Names = drugNames });
        var warnings = drugs.Where(d => !string.IsNullOrWhiteSpace(d.Interactions)).Select(d => new { drug = d.TradeName, warning = d.Interactions });
        return OkResponse(warnings);
    }

    [HttpGet("billing/payments/{id:guid}")]
    public async Task<IActionResult> PaymentById(Guid id)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();
        using var connection = _db.CreateConnection();
        var payment = await connection.QueryFirstOrDefaultAsync("SELECT * FROM dbo.Payments WHERE TenantId = @TenantId AND Id = @Id", new { TenantId = tenantId.Value, Id = id });
        if (payment is null) return Error("Payment not found.", StatusCodes.Status404NotFound);
        var items = await connection.QueryAsync("SELECT * FROM dbo.PaymentItems WHERE PaymentId = @Id", new { Id = id });
        return OkResponse(new { payment, items });
    }

    [HttpGet("billing/patients/{patientId:guid}/payments")]
    public async Task<IActionResult> PatientPayments(Guid patientId)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();
        using var connection = _db.CreateConnection();
        var rows = await connection.QueryAsync("SELECT * FROM dbo.Payments WHERE TenantId = @TenantId AND PatientId = @PatientId ORDER BY CreatedAt DESC", new { TenantId = tenantId.Value, PatientId = patientId });
        return OkResponse(rows);
    }

    [HttpPost("billing/payments/{id:guid}/refund")]
    public async Task<IActionResult> RefundPayment(Guid id, [FromBody] RefundPaymentDto dto)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();
        using var connection = _db.CreateConnection();
        var rows = await connection.ExecuteAsync("UPDATE dbo.Payments SET Status = @Status, RefundedAt = SYSUTCDATETIME(), Notes = CONCAT(COALESCE(Notes, ''), @Reason), UpdatedAt = SYSUTCDATETIME() WHERE TenantId = @TenantId AND Id = @Id", new { TenantId = tenantId.Value, Id = id, Status = PaymentStatus.Refunded, Reason = $" Refund: {dto.Reason}" });
        if (rows == 0) return Error("Payment not found.", StatusCodes.Status404NotFound);
        await Audit("Refund", "Payment", id, dto);
        return OkResponse(true, "Payment refunded.");
    }

    [HttpGet("billing/payments/{id:guid}/receipt")]
    public async Task<IActionResult> ReceiptPdf(Guid id)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();
        using var connection = _db.CreateConnection();
        var payment = await connection.QueryFirstOrDefaultAsync("SELECT InvoiceNumber, TotalAmount, PaidAmount, RemainingAmount, CreatedAt FROM dbo.Payments WHERE TenantId = @TenantId AND Id = @Id", new { TenantId = tenantId.Value, Id = id });
        if (payment is null) return Error("Payment not found.", StatusCodes.Status404NotFound);
        var body = $"Receipt\nInvoice: {payment.InvoiceNumber}\nDate: {payment.CreatedAt:yyyy-MM-dd}\nTotal: {payment.TotalAmount}\nPaid: {payment.PaidAmount}\nRemaining: {payment.RemainingAmount}";
        return File(CreateSimplePdf(body), "application/pdf", $"receipt-{payment.InvoiceNumber}.pdf");
    }

    [HttpGet("billing/debts")]
    public async Task<IActionResult> DebtTracking()
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();
        using var connection = _db.CreateConnection();
        var rows = await connection.QueryAsync(@"
SELECT p.PatientId, pt.FullName, pt.PhoneNumber, SUM(p.RemainingAmount) AS TotalDebt
FROM dbo.Payments p
INNER JOIN dbo.Patients pt ON pt.Id = p.PatientId AND pt.TenantId = p.TenantId
WHERE p.TenantId = @TenantId AND p.RemainingAmount > 0
GROUP BY p.PatientId, pt.FullName, pt.PhoneNumber
ORDER BY TotalDebt DESC;",
            new { TenantId = tenantId.Value });
        return OkResponse(rows);
    }

    [HttpGet("billing/reports/monthly-revenue")]
    public async Task<IActionResult> MonthlyRevenue([FromQuery] int year, [FromQuery] int month)
    {
        var tenantId = RequireTenant();
        if (tenantId is null) return Unauthorized();
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        using var connection = _db.CreateConnection();
        var rows = await connection.QueryAsync(@"
SELECT CAST(CreatedAt AS date) AS [Date], SUM(PaidAmount) AS PaidAmount, SUM(RemainingAmount) AS RemainingAmount, COUNT(1) AS InvoiceCount
FROM dbo.Payments
WHERE TenantId = @TenantId AND CreatedAt >= @Start AND CreatedAt < @End
GROUP BY CAST(CreatedAt AS date)
ORDER BY [Date];",
            new { TenantId = tenantId.Value, Start = start, End = end });
        return OkResponse(rows);
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
        using var connection = _db.CreateConnection();
        var rows = await connection.QueryAsync(@"
SELECT a.*, p.FullName AS PatientName, u.FullName AS DoctorName
FROM dbo.Appointments a
INNER JOIN dbo.Patients p ON p.Id = a.PatientId AND p.TenantId = a.TenantId
INNER JOIN dbo.Users u ON u.Id = a.DoctorId AND u.TenantId = a.TenantId
WHERE a.TenantId = @TenantId AND a.AppointmentDate >= @From AND a.AppointmentDate < @To AND a.IsDeleted = 0
ORDER BY a.AppointmentDate, a.StartTime;",
            new { TenantId = tenantId.Value, From = from, To = to });
        return OkResponse(rows);
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

    private async Task<IActionResult> UserById(System.Data.IDbConnection connection, Guid tenantId, Guid id)
    {
        var user = await connection.QueryFirstOrDefaultAsync<UserListRow>("SELECT * FROM dbo.Users WHERE TenantId = @TenantId AND Id = @Id", new { TenantId = tenantId, Id = id });
        return user is null ? Error("User not found.", StatusCodes.Status404NotFound) : OkResponse(user);
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

    private sealed class UserPasswordRow
    {
        public Guid Id { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
    }

    private sealed class UserListRow
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public string? Phone { get; set; }
        public string? Specialty { get; set; }
        public string? LicenseNumber { get; set; }
        public bool IsActive { get; set; }
    }
}
