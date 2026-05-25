using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Infrastructure.Data;
using Dapper;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Infrastructure.Repositories
{
    public class PatientDocumentRepository : IPatientDocumentRepository
    {
        private readonly DapperContext _context;

        public PatientDocumentRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task AddAsync(PatientDocument document)
        {
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

            using var connection = _context.CreateConnection();
            await connection.ExecuteAsync(sql, document);
        }
    

    public async Task<IEnumerable<PatientDocument>> GetByPatientAsync(Guid tenantId, Guid patientId)
        {
            const string sql = @"
SELECT *
FROM dbo.PatientDocuments
WHERE TenantId = @TenantId
  AND PatientId = @PatientId
ORDER BY UploadedAt DESC;";

            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<PatientDocument>(sql, new
            {
                TenantId = tenantId,
                PatientId = patientId
            });
        }

        public async Task<PatientDocument?> GetByIdAsync(Guid tenantId, Guid patientId, Guid documentId)
        {
            const string sql = @"
SELECT *
FROM dbo.PatientDocuments
WHERE TenantId = @TenantId
  AND PatientId = @PatientId
  AND Id = @DocumentId;";

            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<PatientDocument>(sql, new
            {
                TenantId = tenantId,
                PatientId = patientId,
                DocumentId = documentId
            });
        }
    }
}