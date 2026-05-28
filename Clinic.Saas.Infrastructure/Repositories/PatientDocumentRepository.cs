using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.Interfaces;
using Dapper;

namespace Clinic.Saas.Infrastructure.Repositories
{
    public class PatientDocumentRepository : IPatientDocumentRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public PatientDocumentRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task AddAsync(PatientDocument document)
        {
            if (document.Id == Guid.Empty)
            {
                document.Id = Guid.NewGuid();
            }

            EnsureTenantId(document.TenantId);
            EnsurePatientId(document.PatientId);

            document.FileName = Path.GetFileName(document.FileName);
            document.UploadedAt = document.UploadedAt == default ? DateTime.UtcNow : document.UploadedAt;

            using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                const string validatePatientSql = @"
SELECT COUNT(1)
FROM dbo.Patients
WHERE TenantId = @TenantId
  AND Id = @PatientId
  AND IsDeleted = 0;";

                var patientExists = await connection.ExecuteScalarAsync<int>(
                    validatePatientSql,
                    new
                    {
                        document.TenantId,
                        document.PatientId
                    },
                    transaction);

                if (patientExists == 0)
                {
                    throw new InvalidOperationException("Patient does not belong to this tenant.");
                }

                if (document.VisitId.HasValue)
                {
                    const string validateVisitSql = @"
SELECT COUNT(1)
FROM dbo.Visits
WHERE TenantId = @TenantId
  AND PatientId = @PatientId
  AND Id = @VisitId
  AND IsDeleted = 0;";

                    var visitExists = await connection.ExecuteScalarAsync<int>(
                        validateVisitSql,
                        new
                        {
                            document.TenantId,
                            document.PatientId,
                            document.VisitId
                        },
                        transaction);

                    if (visitExists == 0)
                    {
                        throw new InvalidOperationException("Visit does not belong to this tenant and patient.");
                    }
                }

                const string sql = @"
INSERT INTO dbo.PatientDocuments
(
    Id, TenantId, PatientId, VisitId, FileName, FileUrl, FileSizeKb,
    FileType, DocumentType, Description, UploadedBy, UploadedAt
)
VALUES
(
    @Id, @TenantId, @PatientId, @VisitId, @FileName, @FileUrl, @FileSizeKb,
    @FileType, @DocumentType, @Description, @UploadedBy, @UploadedAt
);";

                await connection.ExecuteAsync(sql, document, transaction);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<IEnumerable<PatientDocument>> GetByPatientAsync(Guid tenantId, Guid patientId)
        {
            EnsureTenantId(tenantId);
            EnsurePatientId(patientId);

            const string sql = @"
SELECT
    Id,
    TenantId,
    PatientId,
    VisitId,
    FileName,
    FileSizeKb,
    FileType,
    DocumentType,
    Description,
    UploadedBy,
    UploadedAt
FROM dbo.PatientDocuments
WHERE TenantId = @TenantId
  AND PatientId = @PatientId
ORDER BY UploadedAt DESC;";

            using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
            return await connection.QueryAsync<PatientDocument>(sql, new
            {
                TenantId = tenantId,
                PatientId = patientId
            });
        }

        public async Task<PatientDocument?> GetByIdAsync(Guid tenantId, Guid patientId, Guid documentId)
        {
            EnsureTenantId(tenantId);
            EnsurePatientId(patientId);

            const string sql = @"
SELECT
    Id,
    TenantId,
    PatientId,
    VisitId,
    FileName,
    FileUrl,
    FileSizeKb,
    FileType,
    DocumentType,
    Description,
    UploadedBy,
    UploadedAt
FROM dbo.PatientDocuments
WHERE TenantId = @TenantId
  AND PatientId = @PatientId
  AND Id = @DocumentId;";

            using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
            return await connection.QueryFirstOrDefaultAsync<PatientDocument>(sql, new
            {
                TenantId = tenantId,
                PatientId = patientId,
                DocumentId = documentId
            });
        }

        private static void EnsureTenantId(Guid tenantId)
        {
            if (tenantId == Guid.Empty)
            {
                throw new InvalidOperationException("TenantId is required.");
            }
        }

        private static void EnsurePatientId(Guid patientId)
        {
            if (patientId == Guid.Empty)
            {
                throw new InvalidOperationException("PatientId is required.");
            }
        }
    }
}
