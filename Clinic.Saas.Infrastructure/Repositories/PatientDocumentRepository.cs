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
    }
}
