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

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
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

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
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

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        
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

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
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

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
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

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        return await connection.QueryAsync<PatientTimelineRow>(sql, new
        {
            TenantId = tenantId,
            PatientId = patientId
        });
    }

    public async Task<IEnumerable<PatientDuplicateRow>> FindDuplicatesAsync(Guid tenantId, string? phone, string? nationalId)
    {
        const string sql = @"
SELECT TOP 20 Id, PatientCode, FullName, PhoneNumber, NationalId
FROM dbo.Patients
WHERE TenantId = @TenantId
  AND IsDeleted = 0
  AND ((@Phone IS NOT NULL AND PhoneNumber = @Phone) OR (@NationalId IS NOT NULL AND NationalId = @NationalId));";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
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

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        return await connection.QueryAsync<PatientExportRow>(sql, new { TenantId = tenantId });
    }

    public async Task<bool> ExistsAsync(Guid tenantId, string phone)
    {
        const string sql = @"
SELECT COUNT(1) FROM dbo.Patients
WHERE TenantId = @TenantId
  AND PhoneNumber = @Phone
  AND IsDeleted = 0;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        var count = await connection.ExecuteScalarAsync<int>(sql, new { TenantId = tenantId, Phone = phone });
        return count > 0;
    }

    public async Task<string> GenerateNextPatientCodeAsync(Guid tenantId)
    {
        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        
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

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
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

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
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
