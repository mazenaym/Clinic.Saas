using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.UseCases.PatientDocuments.Commands
{
    public class UploadPatientDocumentCommand
    {
        public class Command
        {
            public Guid TenantId { get; set; }
            public Guid UserId { get; set; }
            public Guid PatientId { get; set; }
            public string FileName { get; set; } = string.Empty;
            public string ContentType { get; set; } = string.Empty;
            public long FileLength { get; set; }
            public Stream FileStream { get; set; } = Stream.Null;
            public short DocumentType { get; set; } = 1;
            public string? Description { get; set; }
        }

        public class Handler
        {
            private const long MaxPatientDocumentBytes = 10 * 1024 * 1024;

            private readonly IPatientRepository _patientRepository;
            private readonly IPatientDocumentRepository _documentRepository;
            private readonly IFileStorageService _fileStorage;

            public Handler(
                IPatientRepository patientRepository,
                IPatientDocumentRepository documentRepository,
                IFileStorageService fileStorage)
            {
                _patientRepository = patientRepository;
                _documentRepository = documentRepository;
                _fileStorage = fileStorage;
            }

            public async Task<BaseResponse<PatientDocumentUploadResultDto>> Handle(Command command)
            {
                if (command.FileLength <= 0)
                {
                    return Fail("File is empty.", 400);
                }

                if (command.FileLength > MaxPatientDocumentBytes)
                {
                    return Fail("File is too large.", 413);
                }

                if (!Enum.IsDefined(typeof(Domain.Enums.DocumentType), command.DocumentType))
                    return Fail("Document type is invalid.", 400);

                var patient = await _patientRepository.GetByIdAsync(command.TenantId, command.PatientId);
                if (patient is null)
                {
                    return Fail("Patient not found.", 404);
                }

                string storageKey;
                try
                {
                    storageKey = await _fileStorage.SavePatientDocumentAsync(
                        command.TenantId,
                        command.PatientId,
                        command.FileName,
                        command.ContentType,
                        command.FileStream);
                }
                catch (InvalidOperationException ex)
                {
                    return Fail(ex.Message, 400);
                }

                var document = new PatientDocument
                {
                    Id = Guid.NewGuid(),
                    TenantId = command.TenantId,
                    PatientId = command.PatientId,
                    VisitId = null,
                    FileName = Path.GetFileName(command.FileName),
                    FileUrl = storageKey,
                    FileSizeKb = (int)Math.Ceiling((await _fileStorage.GetLengthAsync(storageKey) ?? command.FileLength) / 1024d),
                    FileType = CanonicalContentType(command.FileName),
                    DocumentType = (Domain.Enums.DocumentType)command.DocumentType,
                    Description = command.Description,
                    UploadedBy = command.UserId,
                    UploadedAt = DateTime.UtcNow
                };

                try
                {
                    await _documentRepository.AddAsync(document);
                }
                catch
                {
                    await _fileStorage.DeleteAsync(storageKey);
                    throw;
                }

                return new BaseResponse<PatientDocumentUploadResultDto>
                {
                    Success = true,
                    Message = "Document uploaded successfully.",
                    Data = new PatientDocumentUploadResultDto
                    {
                        Id = document.Id,
                        FileName = document.FileName,
                        FileSizeKb = document.FileSizeKb ?? 0,
                        FileType = document.FileType
                    },
                    StatusCode = 200
                };
            }

            private static BaseResponse<PatientDocumentUploadResultDto> Fail(string message, int statusCode)
            {
                return new BaseResponse<PatientDocumentUploadResultDto>
                {
                    Success = false,
                    Message = message,
                    Errors = [message],
                    StatusCode = statusCode
                };
            }

            private static string CanonicalContentType(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf", ".jpg" or ".jpeg" => "image/jpeg", ".png" => "image/png",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".rtf" => "application/rtf", _ => "application/octet-stream"
            };
        }
    }
}
