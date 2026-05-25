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
    }
}
