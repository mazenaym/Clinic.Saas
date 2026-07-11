using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.UseCases.PatientDocuments.Queries
{
    public class DownloadPatientDocumentQuery
    {
        public class Query
        {
            public Guid TenantId { get; set; }
            public Guid PatientId { get; set; }
            public Guid DocumentId { get; set; }
        }

        public class Handler
        {
            private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
            {
                "application/pdf",
                "image/jpeg",
                "image/png",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/rtf",
                "text/rtf"
            };

            private readonly IPatientDocumentRepository _documentRepository;
            private readonly IFileStorageService _fileStorage;

            public Handler(
                IPatientDocumentRepository documentRepository,
                IFileStorageService fileStorage)
            {
                _documentRepository = documentRepository;
                _fileStorage = fileStorage;
            }

            public async Task<BaseResponse<PatientDocumentDownloadDto>> Handle(Query query)
            {
                var document = await _documentRepository.GetByIdAsync(
                    query.TenantId,
                    query.PatientId,
                    query.DocumentId);

                if (document is null)
                {
                    return new BaseResponse<PatientDocumentDownloadDto>
                    {
                        Success = false,
                        Message = "Document not found.",
                        StatusCode = 404
                    };
                }

                if (string.IsNullOrWhiteSpace(document.FileType) ||
                    !AllowedContentTypes.Contains(document.FileType))
                {
                    return new BaseResponse<PatientDocumentDownloadDto>
                    {
                        Success = false,
                        Message = "Document content type is not allowed.",
                        StatusCode = 400
                    };
                }

                var stream = await _fileStorage.OpenReadAsync(document.FileUrl);
                if (stream is null)
                {
                    return new BaseResponse<PatientDocumentDownloadDto>
                    {
                        Success = false,
                        Message = "File not found.",
                        StatusCode = 404
                    };
                }

                return new BaseResponse<PatientDocumentDownloadDto>
                {
                    Success = true,
                    Message = "OK",
                    Data = new PatientDocumentDownloadDto
                    {
                        FileStream = stream,
                        FileName = SafeFileName(document.FileName),
                        FileType = document.FileType
                    },
                    StatusCode = 200
                };
            }

            private static string SafeFileName(string fileName)
            {
                var safe = Path.GetFileName(fileName);
                foreach (var invalidChar in Path.GetInvalidFileNameChars())
                {
                    safe = safe.Replace(invalidChar, '_');
                }

                return string.IsNullOrWhiteSpace(safe) ? "document" : safe;
            }
        }
    }
}
