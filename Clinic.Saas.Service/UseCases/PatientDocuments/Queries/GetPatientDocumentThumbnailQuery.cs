using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;

namespace Clinic.Saas.Service.UseCases.PatientDocuments.Queries;

public static class GetPatientDocumentThumbnailQuery
{
    public sealed record Query(Guid TenantId, Guid PatientId, Guid DocumentId);
    public sealed class Handler(IPatientDocumentRepository repository, IFileStorageService storage)
    {
        public async Task<BaseResponse<PatientDocumentDownloadDto>> Handle(Query query)
        {
            var document = await repository.GetByIdAsync(query.TenantId, query.PatientId, query.DocumentId);
            if (document is null) return new() { Success = false, Message = "Document not found.", StatusCode = 404 };
            if (!document.FileType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return new() { Success = false, Message = "Thumbnail is only available for images.", StatusCode = 400 };
            var stream = await storage.OpenThumbnailAsync(document.FileUrl);
            if (stream is null) return new() { Success = false, Message = "Thumbnail not found.", StatusCode = 404 };
            return new()
            {
                Success = true, Message = "OK", StatusCode = 200,
                Data = new() { FileStream = stream, FileName = "thumbnail.webp", FileType = "image/webp" }
            };
        }
    }
}
