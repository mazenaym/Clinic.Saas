using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.UseCases.PatientDocuments.Queries
{
    public class GetPatientDocumentsQuery
    {
        public class Query
        {
            public Guid TenantId { get; set; }
            public Guid PatientId { get; set; }
        }

        public class Handler
        {
            private readonly IPatientRepository _patientRepository;
            private readonly IPatientDocumentRepository _documentRepository;

            public Handler(
                IPatientRepository patientRepository,
                IPatientDocumentRepository documentRepository)
            {
                _patientRepository = patientRepository;
                _documentRepository = documentRepository;
            }

            public async Task<BaseResponse<List<PatientDocumentDto>>> Handle(Query query)
            {
                var patient = await _patientRepository.GetByIdAsync(query.TenantId, query.PatientId);
                if (patient is null)
                {
                    return new BaseResponse<List<PatientDocumentDto>>
                    {
                        Success = false,
                        Message = "Patient not found.",
                        StatusCode = 404
                    };
                }

                var documents = await _documentRepository.GetByPatientAsync(
                    query.TenantId,
                    query.PatientId);

                return new BaseResponse<List<PatientDocumentDto>>
                {
                    Success = true,
                    Message = "OK",
                    Data = documents.Select(x => new PatientDocumentDto
                    {
                        Id = x.Id,
                        PatientId = x.PatientId,
                        FileName = x.FileName,
                        FileSizeKb = x.FileSizeKb ?? 0,
                        FileType = x.FileType,
                        DocumentType = (short)x.DocumentType,
                        Description = x.Description,
                        UploadedBy = x.UploadedBy,
                        UploadedAt = x.UploadedAt,
                        RowVersion = x.RowVersion.ToBase64RowVersion()
                    }).ToList(),
                    StatusCode = 200
                };
            }
        }
    }
}
