using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Infrastructure.Data;
using Dapper;

namespace Clinic.Saas.Infrastructure.Repositories;


    public class PatientRepository : IPatientRepository
    {
        private readonly DapperContext _context;

        public PatientRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<Patient?> GetByIdAsync(Guid id)
        {
            var sql = @"
            SELECT * FROM dbo.Patients 
            WHERE Id = @Id AND IsDeleted = 0";

            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Patient>(sql, new { Id = id });
        }

        public async Task<IEnumerable<Patient>> GetAllAsync()
        {
            var sql = "SELECT * FROM dbo.Patients WHERE IsDeleted = 0 ORDER BY CreatedAt DESC";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<Patient>(sql);
        }

    //public async Task<Patient> AddAsync(Patient entity)
    //{
    //    var sql = @"
    //    INSERT INTO dbo.Patients 
    //    (TenantId, FullName, PhoneNumber, DateOfBirth, Gender, BloodType, 
    //     Email, Address, MedicalHistory, DrugAllergies, ChronicDiseases)
    //    OUTPUT INSERTED.*
    //    VALUES 
    //    (@TenantId, @FullName, @PhoneNumber, @DateOfBirth, @Gender, @BloodType,
    //     @Email, @Address, @MedicalHistory, @DrugAllergies, @ChronicDiseases)";

    //    using var connection = _context.CreateConnection();
    //    return await connection.QuerySingleAsync<Patient>(sql, entity);
    //}
    public async Task<Patient> AddAsync(Patient entity)
    {
        // 1. توليد معرف فريد إذا لم يكن موجوداً
        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
        }

        // 2. جملة الـ SQL بدون استخدام OUTPUT
        // قمت بتعديل الأسماء لتطابق الـ Schema التي صممناها (medical_history بدلاً من ChronicDiseases)
        var sql = @"
    INSERT INTO Patients 
    (Id, TenantId,PatientCode, FullName, PhoneNumber, DateOfBirth, Gender, BloodType, 
     Email, Address, MedicalHistory, DrugAllergies)
    VALUES 
    (@Id, @TenantId, @PatientCode, @FullName, @PhoneNumber, @DateOfBirth, @Gender, @BloodType,
     @Email, @Address, @MedicalHistory, @DrugAllergies);

    -- 3. جلب السجل الذي تم إنشاؤه في نفس الطلب
    SELECT * FROM Patients WHERE Id = @Id;";

        using var connection = _context.CreateConnection();

        // تنفيذ الاستعلام وجلب النتيجة
        return await connection.QuerySingleAsync<Patient>(sql, entity);
    }

    public async Task UpdateAsync(Patient entity)
        {
            var sql = @"
            UPDATE dbo.Patients 
            SET FullName = @FullName,
                PhoneNumber = @PhoneNumber,
                DateOfBirth = @DateOfBirth,
                Gender = @Gender,
                BloodType = @BloodType,
                Email = @Email,
                Address = @Address,
                MedicalHistory = @MedicalHistory,
                DrugAllergies = @DrugAllergies,
                ChronicDiseases = @ChronicDiseases
            WHERE Id = @Id";

            using var connection = _context.CreateConnection();
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            var sql = "UPDATE dbo.Patients SET IsDeleted = 1 WHERE Id = @Id";

            using var connection = _context.CreateConnection();
            await connection.ExecuteAsync(sql, new { Id = id });
        }

        public async Task<Patient?> GetByPhoneAsync(Guid tenantId, string phone)
        {
            var sql = @"
            SELECT * FROM dbo.Patients 
            WHERE TenantId = @TenantId 
            AND PhoneNumber = @Phone 
            AND IsDeleted = 0";

            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Patient>(
                sql,
                new { TenantId = tenantId, Phone = phone }
            );
        }

        public async Task<IEnumerable<Patient>> SearchAsync(Guid tenantId, string searchTerm)
        {
            var sql = @"
            SELECT * FROM dbo.Patients 
            WHERE TenantId = @TenantId 
            AND IsDeleted = 0
            AND (
                FullName LIKE @Search 
                OR PhoneNumber LIKE @Search 
                OR PatientCode LIKE @Search
            )
            ORDER BY FullName";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<Patient>(
                sql,
                new { TenantId = tenantId, Search = $"%{searchTerm}%" }
            );
        }

        public async Task<bool> ExistsAsync(Guid tenantId, string phone)
        {
            var sql = @"
            SELECT COUNT(1) FROM dbo.Patients 
            WHERE TenantId = @TenantId 
            AND PhoneNumber = @Phone 
            AND IsDeleted = 0";

            using var connection = _context.CreateConnection();
            var count = await connection.ExecuteScalarAsync<int>(
                sql,
                new { TenantId = tenantId, Phone = phone }
            );
            return count > 0;
        }

        public async Task<string> GenerateNextPatientCodeAsync(Guid tenantId)
        {
            var sql = @"
            SELECT ISNULL(MAX(TRY_CAST(SUBSTRING(PatientCode, 5, LEN(PatientCode) - 4) AS INT)), 0) + 1
            FROM dbo.Patients
            WHERE TenantId = @TenantId
              AND PatientCode LIKE 'CLN-%';";

            using var connection = _context.CreateConnection();
            var nextNumber = await connection.ExecuteScalarAsync<int>(sql, new { TenantId = tenantId });

            return $"CLN-{nextNumber:D5}";
        }
    }
