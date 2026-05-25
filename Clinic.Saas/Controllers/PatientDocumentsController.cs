using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.UseCases.PatientDocuments.Commands;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Saas.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PatientDocumentsController : ControllerBase
    {
        private readonly UploadPatientDocumentCommand.Handler _uploadDocument;
        private readonly ICurrentUserService _currentUser;

        public PatientDocumentsController(
            UploadPatientDocumentCommand.Handler uploadDocument,
            ICurrentUserService currentUser)
        {
            _uploadDocument = uploadDocument;
            _currentUser = currentUser;
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

            return StatusCode(result.StatusCode, result);
        }
    }
}
