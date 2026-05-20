using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Infrastructure.Data;
using Dapper;
using System.Data;

namespace Clinic.Saas.Infrastructure.Repositories;

public class PatientRepository : IPatientRepository
{
    private readonly DapperContext _context;

    public PatientRepository(DapperContext context)
    {
        _context = context;
    }

    public Task<Patient?> GetByIdAsync(Guid id) => GetByIdInternalAsync(id);

    public async Task<Patient?> GetByIdAsync(Guid tenantId, Guid id)
    {
        const string sql = @"
SELECT * FROM dbo.Patients
WHERE TenantId = @TenantId
  AND Id = @Id
  AND IsDeleted = 0;";

        using var connection = _context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Patient>(sql, new { TenantId = tenantId, Id = id });
    }

    public Task<IEnumerable<Patient>> GetAllAsync() => GetAllInternalAsync(null);

    public Task<IEnumerable<Patient>> GetAllAsync(Guid tenantId) => GetAllInternalAsync(tenantId);

    public async Task<Patient> AddAsync(Patient entity)
    {
        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
        }

        entity.CreatedAt = entity.CreatedAt == default ? DateTime.UtcNow : entity.CreatedAt;
        entity.UpdatedAt = entity.UpdatedAt == default ? entity.CreatedAt : entity.UpdatedAt;

        using var connection = _context.CreateConnection();
        connection.Open();
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

    public Task UpdateAsync(Patient entity) => UpdateInternalAsync(null, entity);

    public Task UpdateAsync(Guid tenantId, Patient entity) => UpdateInternalAsync(tenantId, entity);

    public Task DeleteAsync(Guid id) => DeleteInternalAsync(null, id);

    public Task DeleteAsync(Guid tenantId, Guid id) => DeleteInternalAsync(tenantId, id);

    public async Task<Patient?> GetByPhoneAsync(Guid tenantId, string phone)
    {
        const string sql = @"
SELECT * FROM dbo.Patients
WHERE TenantId = @TenantId
  AND PhoneNumber = @Phone
  AND IsDeleted = 0;";

        using var connection = _context.CreateConnection();
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

        using var connection = _context.CreateConnection();
        return await connection.QueryAsync<Patient>(sql, new { TenantId = tenantId, Search = $"%{searchTerm}%" });
    }

    public async Task<bool> ExistsAsync(Guid tenantId, string phone)
    {
        const string sql = @"
SELECT COUNT(1) FROM dbo.Patients
WHERE TenantId = @TenantId
  AND PhoneNumber = @Phone
  AND IsDeleted = 0;";

        using var connection = _context.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(sql, new { TenantId = tenantId, Phone = phone });
        return count > 0;
    }

    public async Task<string> GenerateNextPatientCodeAsync(Guid tenantId)
    {
        using var connection = _context.CreateConnection();
        connection.Open();
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

    private async Task<Patient?> GetByIdInternalAsync(Guid id)
    {
        const string sql = @"SELECT * FROM dbo.Patients WHERE Id = @Id AND IsDeleted = 0;";
        using var connection = _context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Patient>(sql, new { Id = id });
    }

    private async Task<IEnumerable<Patient>> GetAllInternalAsync(Guid? tenantId)
    {
        const string sql = @"
SELECT * FROM dbo.Patients
WHERE IsDeleted = 0
  AND (@TenantId IS NULL OR TenantId = @TenantId)
ORDER BY CreatedAt DESC;";

        using var connection = _context.CreateConnection();
        return await connection.QueryAsync<Patient>(sql, new { TenantId = tenantId });
    }

    private async Task UpdateInternalAsync(Guid? tenantId, Patient entity)
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
  AND (@TenantIdFilter IS NULL OR TenantId = @TenantIdFilter);";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            entity.Id,
            TenantIdFilter = tenantId,
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
    }

    private async Task DeleteInternalAsync(Guid? tenantId, Guid id)
    {
        const string sql = @"
UPDATE dbo.Patients
SET IsDeleted = 1, UpdatedAt = SYSUTCDATETIME()
WHERE Id = @Id
  AND (@TenantId IS NULL OR TenantId = @TenantId);";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { TenantId = tenantId, Id = id });
    }
}
