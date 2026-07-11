using Clinic.Saas.Service.Services;
using Microsoft.Extensions.Configuration;

namespace Clinic.Saas.Tests;

public sealed class MediaPipelineTests
{
    [Fact]
    public void Pdf_service_generates_a_real_pdf_with_unicode_content()
    {
        var content = new PdfDocumentService().Generate("فاتورة", [("المريض", "أحمد محمد")], ["كشف طبي — 500.00"]);
        Assert.True(content.Length > 1_000);
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(content, 0, 5));
    }

    [Fact]
    public async Task Storage_rejects_spoofed_pdf_and_path_traversal()
    {
        var root = Path.Combine(Path.GetTempPath(), $"clinic-media-{Guid.NewGuid():N}");
        try
        {
            var configuration = new ConfigurationManager { ["FileStorage:RootPath"] = root };
            var storage = new LocalFileStorageService(configuration);
            await Assert.ThrowsAsync<InvalidOperationException>(() => storage.SavePatientDocumentAsync(
                Guid.NewGuid(), Guid.NewGuid(), "fake.pdf", "application/pdf", new MemoryStream("not a pdf"u8.ToArray())));
            Assert.Null(await storage.OpenReadAsync("storage/../../secret.txt"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
