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
            var token = await storage.StageDeleteAsync(document.FileUrl);
            if (token is null)
                return new() { Success = false, Message = "Stored file is missing; database record was preserved.", StatusCode = 409 };
            try
            {
                if (!await repository.DeleteAsync(command.TenantId, command.PatientId, command.DocumentId))
                {
                    await storage.RestoreStagedDeleteAsync(token);
                    return new() { Success = false, Message = "Document could not be deleted.", StatusCode = 409 };
                }
            }
            catch
            {
                await storage.RestoreStagedDeleteAsync(token);
                return new() { Success = false, Message = "Document deletion failed; no data was removed.", StatusCode = 500 };
            }

            if (await storage.CommitStagedDeleteAsync(token))
                return new() { Success = true, Data = true, Message = "Document deleted.", StatusCode = 200 };

            try
            {
                await repository.AddAsync(document);
                await storage.RestoreStagedDeleteAsync(token);
            }
            catch { /* staged files are cleaned by the storage recovery sweep */ }
            return new() { Success = false, Message = "Document deletion could not be completed.", StatusCode = 500 };
        }
    }
}
