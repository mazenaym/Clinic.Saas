using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.UseCases.PatientDocuments.Commands;
using Clinic.Saas.Service.UseCases.PatientDocuments.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Clinic.Saas.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Doctor,Reception")]
    public class PatientDocumentsController : ControllerBase
    {
        private readonly UploadPatientDocumentCommand.Handler _uploadDocument;
        private readonly GetPatientDocumentsQuery.Handler _getDocuments;
        private readonly DownloadPatientDocumentQuery.Handler _downloadDocument;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditService _auditService;

        public PatientDocumentsController(
            UploadPatientDocumentCommand.Handler uploadDocument,
            GetPatientDocumentsQuery.Handler getDocuments,
            DownloadPatientDocumentQuery.Handler downloadDocument,
            ICurrentUserService currentUser,
            IAuditService auditService)
        {
            _uploadDocument = uploadDocument;
            _getDocuments = getDocuments;
            _downloadDocument = downloadDocument;
            _currentUser = currentUser;
            _auditService = auditService;
        }

        [HttpPost]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> Upload(
            Guid patientId,
            IFormFile file,
            [FromForm] short documentType = 1,
            [FromForm] string? description = null)
        {
            if (!_currentUser.TenantId.HasValue || !_currentUser.UserId.HasValue)
            {
                return Unauthorized();
            }

            if (file is null)
            {
                return BadRequest(new BaseResponse<object>
                {
                    Success = false,
                    Message = "File is required.",
                    Errors = ["File is required."],
                    StatusCode = 400
                });
            }

            await using var stream = file.OpenReadStream();

            var result = await _uploadDocument.Handle(new UploadPatientDocumentCommand.Command
            {
                TenantId = _currentUser.TenantId.Value,
                UserId = _currentUser.UserId.Value,
                PatientId = patientId,
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileLength = file.Length,
                FileStream = stream,
                DocumentType = documentType,
                Description = description
            });

            if (result.Success)
            {
                await this.AuditAsync(_auditService, _currentUser, "Upload", "PatientDocument", result.Data?.Id, new { result.Data?.Id, patientId });
            }

            return StatusCode(result.StatusCode, result);
        }
        [HttpGet]
        public async Task<IActionResult> GetDocuments(Guid patientId)
        {
            if (!_currentUser.TenantId.HasValue)
            {
                return Unauthorized();
            }

            var result = await _getDocuments.Handle(new GetPatientDocumentsQuery.Query
            {
                TenantId = _currentUser.TenantId.Value,
                PatientId = patientId
            });

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{documentId:guid}/download")]
        public async Task<IActionResult> Download(Guid patientId, Guid documentId)
        {
            if (!_currentUser.TenantId.HasValue)
            {
                return Unauthorized();
            }

            var result = await _downloadDocument.Handle(new DownloadPatientDocumentQuery.Query
            {
                TenantId = _currentUser.TenantId.Value,
                PatientId = patientId,
                DocumentId = documentId
            });

            if (!result.Success || result.Data is null)
            {
                return StatusCode(result.StatusCode, result);
            }

            await this.AuditAsync(_auditService, _currentUser, "Download", "PatientDocument", documentId, new { documentId, patientId });

            return File(
                result.Data.FileStream,
                result.Data.FileType,
                result.Data.FileName);
        }

        [HttpGet("{documentId:guid}/view")]
        public async Task<IActionResult> View(Guid patientId, Guid documentId)
        {
            if (!_currentUser.TenantId.HasValue)
            {
                return Unauthorized();
            }

            var result = await _downloadDocument.Handle(new DownloadPatientDocumentQuery.Query
            {
                TenantId = _currentUser.TenantId.Value,
                PatientId = patientId,
                DocumentId = documentId
            });

            if (!result.Success || result.Data is null)
            {
                return StatusCode(result.StatusCode, result);
            }

            await this.AuditAsync(_auditService, _currentUser, "View", "PatientDocument", documentId, new { documentId, patientId });

            Response.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline")
            {
                FileNameStar = result.Data.FileName
            }.ToString();

            return File(result.Data.FileStream, result.Data.FileType);
        }
    }
}

