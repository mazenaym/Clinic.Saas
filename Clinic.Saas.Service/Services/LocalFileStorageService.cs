using Clinic.Saas.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.Services
{
    public class LocalFileStorageService : IFileStorageService
    {
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".jpg",
        ".jpeg",
        ".png"
    };

        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png"
    };

        public async Task<string> SavePatientDocumentAsync(
            Guid tenantId,
            Guid patientId,
            string originalFileName,
            string contentType,
            Stream fileStream,
            CancellationToken cancellationToken = default)
        {
            var extension = Path.GetExtension(originalFileName);

            if (string.IsNullOrWhiteSpace(extension) ||
                !AllowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException("File type is not allowed.");
            }

            if (string.IsNullOrWhiteSpace(contentType) ||
                !AllowedContentTypes.Contains(contentType))
            {
                throw new InvalidOperationException("File content type is not allowed.");
            }

            var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

            var uploadsRoot = Path.Combine(AppContext.BaseDirectory, "uploads");
            var relativeDirectory = Path.Combine(
                tenantId.ToString("N"),
                "patients",
                patientId.ToString("N"));

            var absoluteDirectory = Path.Combine(uploadsRoot, relativeDirectory);
            Directory.CreateDirectory(absoluteDirectory);

            var fullPath = Path.Combine(absoluteDirectory, storedFileName);

            await using (var output = File.Create(fullPath))
            {
                await fileStream.CopyToAsync(output, cancellationToken);
            }

            return string.Join('/',
                "uploads",
                tenantId.ToString("N"),
                "patients",
                patientId.ToString("N"),
                storedFileName);
        }
    }
}
