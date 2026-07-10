using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Exceptions;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Infrastructure.Data;
using Clinic.Saas.Service.Interfaces;
using Dapper;
using System.Data;

namespace Clinic.Saas.Infrastructure.Repositories;

public class PatientRepository : IPatientRepository
{
    //private readonly DapperContext _context;

    //public PatientRepository(DapperContext context)
    //{
    //    _context = context;
    //}
    private readonly IDbConnectionFactory _connectionFactory;

    public PatientRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public Task<Patient?> GetByIdAsync(Guid id) =>
    throw new NotSupportedException("Use GetByIdAsync(Guid tenantId, Guid id) for tenant-owned data.");

    public async Task<Patient?> GetByIdAsync(Guid tenantId, Guid id)
    {
        const string sql = @"
SELECT * FROM dbo.Patients
WHERE TenantId = @TenantId
  AND Id = @Id
  AND IsDeleted = 0;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        return await connection.QueryFirstOrDefaultAsync<Patient>(sql, new { TenantId = tenantId, Id = id });
    }

    public Task<IEnumerable<Patient>> GetAllAsync() =>
    throw new NotSupportedException("Use GetAllAsync(Guid tenantId) for tenant-owned data.");

    public async Task<IEnumerable<Patient>> GetAllAsync(Guid tenantId)
    {
        const string sql = @"
SELECT * FROM dbo.Patients
WHERE TenantId = @TenantId
  AND IsDeleted = 0
ORDER BY CreatedAt DESC;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        return await connection.QueryAsync<Patient>(sql, new { TenantId = tenantId });
    }

    public async Task<Patient> AddAsync(Patient entity)
    {
        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
        }

        if (entity.TenantId == Guid.Empty)
        {
            throw new InvalidOperationException("TenantId is required.");
        }

        entity.CreatedAt = entity.CreatedAt == default ? DateTime.UtcNow : entity.CreatedAt;
        entity.UpdatedAt = entity.UpdatedAt == default ? entity.CreatedAt : entity.UpdatedAt;

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(entity.TenantId);
        
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            if (string.IsNullOrWhiteSpace(entity.PatientCode))
            {
                entity.PatientCode = await GenerateNextPatientCodeAsync(connection, transaction, entity.TenantId);
            }

            const string sql = @"
INSERT INTO dbo.Patients
(
    Id, TenantId, PatientCode, FullName, PhoneNumber, DateOfBirth, Gender, BloodType,
    NationalId, Email, Address, MedicalHistory, DrugAllergies, ChronicDiseases,
    EmergencyContactName, EmergencyContactPhone, InsuranceCompany, InsuranceNumber,
    IsActive, IsDeleted, CreatedAt, UpdatedAt, CreatedBy
)
VALUES
(
    @Id, @TenantId, @PatientCode, @FullName, @PhoneNumber, @DateOfBirth, @Gender, @BloodType,
    @NationalId, @Email, @Address, @MedicalHistory, @DrugAllergies, @ChronicDiseases,
    @EmergencyContactName, @EmergencyContactPhone, @InsuranceCompany, @InsuranceNumber,
    @IsActive, @IsDeleted, @CreatedAt, @UpdatedAt, @CreatedBy
);";

            await connection.ExecuteAsync(sql, entity, transaction);
            transaction.Commit();

            var created = await GetByIdAsync(entity.TenantId, entity.Id);
            return created ?? throw new InvalidOperationException("Patient was not found after creation.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public Task UpdateAsync(Patient entity) =>
    throw new NotSupportedException("Use UpdateAsync(Guid tenantId, Patient entity) for tenant-owned data.");

    
 
    public Task DeleteAsync(Guid id) =>
    throw new NotSupportedException("Use DeleteAsync(Guid tenantId, Guid id) for tenant-owned data.");

    public async Task<Patient?> GetByPhoneAsync(Guid tenantId, string phone)
    {
        const string sql = @"
SELECT * FROM dbo.Patients
WHERE TenantId = @TenantId
  AND PhoneNumber = @Phone
  AND IsDeleted = 0;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        return await connection.QueryFirstOrDefaultAsync<Patient>(sql, new { TenantId = tenantId, Phone = phone });
    }

    public async Task<IEnumerable<Patient>> SearchAsync(Guid tenantId, string searchTerm)
    {
        const string sql = @"
SELECT * FROM dbo.Patients
WHERE TenantId = @TenantId
  AND IsDeleted = 0
  AND (
      @Search = '%%'
      OR FullName LIKE @Search
      OR PhoneNumber LIKE @Search
      OR PatientCode LIKE @Search
  )
ORDER BY FullName;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        return await connection.QueryAsync<Patient>(sql, new { TenantId = tenantId, Search = $"%{searchTerm}%" });
    }

    public async Task<IEnumerable<PatientTimelineRow>> GetTimelineAsync(Guid tenantId, Guid patientId)
    {
        const string sql = @"
SELECT 'Appointment' AS [Type],
       Id,
       CAST(AppointmentDate AS datetime2) AS [Date],
       CONCAT(N'Appointment ', CAST([Status] AS nvarchar(10))) AS Title,
       Notes AS Details
FROM dbo.Appointments
WHERE TenantId = @TenantId
  AND PatientId = @PatientId
  AND IsDeleted = 0
UNION ALL
SELECT 'Visit',
       Id,
       VisitDate,
       ChiefComplaint,
       Diagnosis
FROM dbo.Visits
WHERE TenantId = @TenantId
  AND PatientId = @PatientId
  AND IsDeleted = 0
UNION ALL
SELECT 'Prescription',
       Id,
       CreatedAt,
       N'Prescription',
       Notes
FROM dbo.Prescriptions
WHERE TenantId = @TenantId
  AND PatientId = @PatientId
  AND IsActive = 1
UNION ALL
SELECT 'Payment',
       Id,
       CreatedAt,
       InvoiceNumber,
       CONCAT(N'Paid ', PaidAmount, N' / Total ', TotalAmount)
FROM dbo.Payments
WHERE TenantId = @TenantId
  AND PatientId = @PatientId
ORDER BY [Date] DESC;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        return await connection.QueryAsync<PatientTimelineRow>(sql, new
        {
            TenantId = tenantId,
            PatientId = patientId
        });
    }

    public async Task<PatientChartData> GetChartAsync(Guid tenantId, Guid patientId)
    {
        const string sql = @"
SELECT
    Id,
    PatientCode,
    FullName,
    PhoneNumber,
    DateOfBirth,
    Gender,
    BloodType,
    Email,
    Address,
    InsuranceCompany,
    DrugAllergies,
    ChronicDiseases,
    CreatedAt,
    RowVersion
FROM dbo.Patients
WHERE TenantId = @TenantId
  AND Id = @PatientId
  AND IsDeleted = 0;

SELECT TOP 5
    v.Id,
    v.VisitDate,
    v.VisitType,
    v.ChiefComplaint,
    v.Diagnosis,
    v.DiagnosisCode,
    u.FullName AS DoctorName,
    v.RowVersion
FROM dbo.Visits v
INNER JOIN dbo.Users u ON u.Id = v.DoctorId AND u.TenantId = v.TenantId
WHERE v.TenantId = @TenantId
  AND v.PatientId = @PatientId
  AND v.IsDeleted = 0
ORDER BY v.VisitDate DESC;

SELECT TOP 5
    pr.Id,
    pr.CreatedAt,
    u.FullName AS DoctorName,
    COALESCE(items.ItemCount, 0) AS ItemCount,
    items.ItemsSummary,
    pr.SentViaWhatsapp,
    pr.RowVersion
FROM dbo.Prescriptions pr
INNER JOIN dbo.Users u ON u.Id = pr.DoctorId AND u.TenantId = pr.TenantId
OUTER APPLY
(
    SELECT COUNT(1) AS ItemCount,
           STRING_AGG(CAST(pi.DrugName AS nvarchar(max)), N', ') AS ItemsSummary
    FROM dbo.PrescriptionItems pi
    WHERE pi.PrescriptionId = pr.Id
) items
WHERE pr.TenantId = @TenantId
  AND pr.PatientId = @PatientId
  AND pr.IsActive = 1
ORDER BY pr.CreatedAt DESC;

SELECT TOP 5
    a.Id,
    CAST(a.AppointmentDate AS datetime2) AS AppointmentDate,
    a.StartTime,
    a.EndTime,
    a.Status,
    a.Type,
    u.FullName AS DoctorName,
    a.Notes,
    a.RowVersion
FROM dbo.Appointments a
INNER JOIN dbo.Users u ON u.Id = a.DoctorId AND u.TenantId = a.TenantId
WHERE a.TenantId = @TenantId
  AND a.PatientId = @PatientId
  AND a.IsDeleted = 0
ORDER BY a.AppointmentDate DESC, a.StartTime DESC;

SELECT
    COUNT(1) AS InvoiceCount,
    COALESCE(SUM(PaidAmount), 0) AS TotalPaid,
    COALESCE(SUM(RemainingAmount), 0) AS TotalOutstanding,
    MAX(CreatedAt) AS LastPaymentAt
FROM dbo.Payments
WHERE TenantId = @TenantId
  AND PatientId = @PatientId;

SELECT TOP 10
    Id,
    VisitId,
    FileName,
    COALESCE(FileSizeKb, 0) AS FileSizeKb,
    FileType,
    DocumentType,
    Description,
    UploadedAt,
    RowVersion
FROM dbo.PatientDocuments
WHERE TenantId = @TenantId
  AND PatientId = @PatientId
ORDER BY UploadedAt DESC;

SELECT TOP 20 *
FROM
(
    SELECT 'Appointment' AS [Type],
           Id,
           CAST(AppointmentDate AS datetime2) AS [Date],
           CONCAT(N'Appointment ', CAST([Status] AS nvarchar(10))) AS Title,
           Notes AS Details
    FROM dbo.Appointments
    WHERE TenantId = @TenantId
      AND PatientId = @PatientId
      AND IsDeleted = 0
    UNION ALL
    SELECT 'Visit',
           Id,
           VisitDate,
           ChiefComplaint,
           Diagnosis
    FROM dbo.Visits
    WHERE TenantId = @TenantId
      AND PatientId = @PatientId
      AND IsDeleted = 0
    UNION ALL
    SELECT 'Prescription',
           Id,
           CreatedAt,
           N'Prescription',
           NULL
    FROM dbo.Prescriptions
    WHERE TenantId = @TenantId
      AND PatientId = @PatientId
      AND IsActive = 1
    UNION ALL
    SELECT 'Payment',
           Id,
           CreatedAt,
           N'Payment',
           NULL
    FROM dbo.Payments
    WHERE TenantId = @TenantId
      AND PatientId = @PatientId
) timeline
ORDER BY [Date] DESC;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        using var multi = await connection.QueryMultipleAsync(sql, new
        {
            TenantId = tenantId,
            PatientId = patientId
        });

        return new PatientChartData
        {
            Patient = await multi.ReadFirstOrDefaultAsync<PatientChartPatientRow>(),
            RecentVisits = (await multi.ReadAsync<PatientChartVisitRow>()).ToList(),
            RecentPrescriptions = (await multi.ReadAsync<PatientChartPrescriptionRow>()).ToList(),
            RecentAppointments = (await multi.ReadAsync<PatientChartAppointmentRow>()).ToList(),
            PaymentSummary = await multi.ReadFirstOrDefaultAsync<PatientChartPaymentSummaryRow>() ?? new PatientChartPaymentSummaryRow(),
            Documents = (await multi.ReadAsync<PatientChartDocumentRow>()).ToList(),
            Timeline = (await multi.ReadAsync<PatientTimelineRow>()).ToList()
        };
    }

    public async Task<IEnumerable<PatientDuplicateRow>> FindDuplicatesAsync(Guid tenantId, string? phone, string? nationalId)
    {
        const string sql = @"
SELECT TOP 20 Id, PatientCode, FullName, PhoneNumber, NationalId
FROM dbo.Patients
WHERE TenantId = @TenantId
  AND IsDeleted = 0
  AND ((@Phone IS NOT NULL AND PhoneNumber = @Phone) OR (@NationalId IS NOT NULL AND NationalId = @NationalId));";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        return await connection.QueryAsync<PatientDuplicateRow>(sql, new
        {
            TenantId = tenantId,
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone,
            NationalId = string.IsNullOrWhiteSpace(nationalId) ? null : nationalId
        });
    }

    public async Task<IEnumerable<PatientExportRow>> GetExportRowsAsync(Guid tenantId)
    {
        const string sql = @"
SELECT PatientCode, FullName, PhoneNumber, NationalId, Email, Gender, CreatedAt
FROM dbo.Patients
WHERE TenantId = @TenantId
  AND IsDeleted = 0
ORDER BY CreatedAt DESC;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        return await connection.QueryAsync<PatientExportRow>(sql, new { TenantId = tenantId });
    }

    public async Task<bool> ExistsAsync(Guid tenantId, string phone)
    {
        const string sql = @"
SELECT COUNT(1) FROM dbo.Patients
WHERE TenantId = @TenantId
  AND PhoneNumber = @Phone
  AND IsDeleted = 0;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        var count = await connection.ExecuteScalarAsync<int>(sql, new { TenantId = tenantId, Phone = phone });
        return count > 0;
    }

    public async Task<string> GenerateNextPatientCodeAsync(Guid tenantId)
    {
        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var code = await GenerateNextPatientCodeAsync(connection, transaction, tenantId);
        transaction.Commit();
        return code;
    }

    private static async Task<string> GenerateNextPatientCodeAsync(IDbConnection connection, IDbTransaction transaction, Guid tenantId)
    {
        const string sql = @"
SELECT ISNULL(MAX(TRY_CAST(SUBSTRING(PatientCode, 5, LEN(PatientCode) - 4) AS INT)), 0) + 1
FROM dbo.Patients WITH (UPDLOCK, HOLDLOCK)
WHERE TenantId = @TenantId
  AND PatientCode LIKE 'CLN-%';";

        var nextNumber = await connection.ExecuteScalarAsync<int>(sql, new { TenantId = tenantId }, transaction);
        return $"CLN-{nextNumber:D5}";
    }

    

    public async Task UpdateAsync(Guid tenantId, Patient entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;

        const string sql = @"
UPDATE dbo.Patients
SET FullName = @FullName,
    PhoneNumber = @PhoneNumber,
    DateOfBirth = @DateOfBirth,
    Gender = @Gender,
    BloodType = @BloodType,
    NationalId = @NationalId,
    Email = @Email,
    Address = @Address,
    MedicalHistory = @MedicalHistory,
    DrugAllergies = @DrugAllergies,
    ChronicDiseases = @ChronicDiseases,
    EmergencyContactName = @EmergencyContactName,
    EmergencyContactPhone = @EmergencyContactPhone,
    InsuranceCompany = @InsuranceCompany,
    InsuranceNumber = @InsuranceNumber,
    UpdatedAt = @UpdatedAt
WHERE Id = @Id
  AND TenantId = @TenantId
  AND RowVersion = @RowVersion;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        var rows = await connection.ExecuteAsync(sql, new
        {
            entity.Id,
            TenantId = tenantId,
            entity.RowVersion,
            entity.FullName,
            entity.PhoneNumber,
            entity.DateOfBirth,
            entity.Gender,
            entity.BloodType,
            entity.NationalId,
            entity.Email,
            entity.Address,
            entity.MedicalHistory,
            entity.DrugAllergies,
            entity.ChronicDiseases,
            entity.EmergencyContactName,
            entity.EmergencyContactPhone,
            entity.InsuranceCompany,
            entity.InsuranceNumber,
            entity.UpdatedAt
        });

        await ThrowIfConcurrencyConflictAsync(connection, tenantId, entity.Id, rows, "Patient");
    }

    public async Task DeleteAsync(Guid tenantId, Guid id, byte[] rowVersion)
    {
        const string sql = @"
UPDATE dbo.Patients
SET IsDeleted = 1, UpdatedAt = SYSUTCDATETIME()
WHERE Id = @Id
  AND TenantId = @TenantId
  AND RowVersion = @RowVersion;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        var rows = await connection.ExecuteAsync(sql, new
        {
            TenantId = tenantId,
            Id = id,
            RowVersion = rowVersion
        });

        await ThrowIfConcurrencyConflictAsync(connection, tenantId, id, rows, "Patient");
    }

    private static async Task ThrowIfConcurrencyConflictAsync(IDbConnection connection, Guid tenantId, Guid id, int rows, string entityName)
    {
        if (rows > 0)
        {
            return;
        }

        const string existsSql = @"
SELECT COUNT(1)
FROM dbo.Patients
WHERE TenantId = @TenantId
  AND Id = @Id
  AND IsDeleted = 0;";

        var exists = await connection.ExecuteScalarAsync<int>(existsSql, new { TenantId = tenantId, Id = id });
        if (exists > 0)
        {
            throw new ConcurrencyConflictException($"{entityName} was modified by another request.");
        }

        throw new RecordNotFoundException($"{entityName} was not found.");
    }

}
