using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;

namespace Clinic.Saas.Service.UseCases.PatientDocuments.Commands;

public static class DeletePatientDocumentCommand
{
    public sealed record Command(Guid TenantId, Guid PatientId, Guid DocumentId);

    public sealed class Handler(IPatientDocumentRepository repository, IFileStorageService storage)
    {
        public async Task<BaseResponse<bool>> Handle(Command command)
        {
            var document = await repository.GetByIdAsync(command.TenantId, command.PatientId, command.DocumentId);
            if (document is null) return new() { Success = false, Message = "Document not found.", StatusCode = 404 };
            if (!await repository.DeleteAsync(command.TenantId, command.PatientId, command.DocumentId))
                return new() { Success = false, Message = "Document could not be deleted.", StatusCode = 409 };
            await storage.DeleteAsync(document.FileUrl);
            return new() { Success = true, Data = true, Message = "Document deleted.", StatusCode = 200 };
        }
    }
}
