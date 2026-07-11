using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> SavePatientDocumentAsync(
         Guid tenantId,
         Guid patientId,
         string originalFileName,
         string contentType,
         Stream fileStream,
         CancellationToken cancellationToken = default);

        Task<Stream?> OpenReadAsync(
            string storageKey,
            CancellationToken cancellationToken = default);

        Task<StoredImage> SaveImageAsync(
            Guid tenantId,
            string category,
            Guid ownerId,
            string originalFileName,
            Stream fileStream,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        Task<bool> DeleteAsync(string storageKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        Task<long?> GetLengthAsync(string storageKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<long?>(null);
    }

    public sealed record StoredImage(string StorageKey, string ContentType, long Length);
}
