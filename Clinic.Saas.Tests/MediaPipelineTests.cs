using Clinic.Saas.Service.Services;
using Microsoft.Extensions.Configuration;
using SkiaSharp;
using System.IO.Compression;
using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.UseCases.PatientDocuments.Commands;
using NSubstitute;

namespace Clinic.Saas.Tests;

public sealed class MediaPipelineTests
{
    [Fact]
    public void Pdf_service_generates_a_real_pdf_with_unicode_content()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var content = new PdfDocumentService(new ConfigurationManager()).Generate("فاتورة", [("المريض", "أحمد محمد")], ["كشف طبي — 500.00"]);
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

    [Fact]
    public async Task Storage_creates_small_thumbnail_and_supports_compensating_delete()
    {
        var (storage, root) = CreateStorage();
        try
        {
            using var bitmap = new SKBitmap(1800, 1200);
            bitmap.Erase(SKColors.CornflowerBlue);
            using var image = SKImage.FromBitmap(bitmap);
            using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, 95);
            await using var input = encoded.AsStream();
            var key = await storage.SavePatientDocumentAsync(Guid.NewGuid(), Guid.NewGuid(), "صورة أشعة.jpg", "image/jpeg", input);
            long originalLength;
            long thumbnailLength;
            await using (var original = await storage.OpenReadAsync(key)) { Assert.NotNull(original); originalLength = original!.Length; }
            await using (var thumbnail = await storage.OpenThumbnailAsync(key)) { Assert.NotNull(thumbnail); thumbnailLength = thumbnail!.Length; }
            Assert.True(thumbnailLength < originalLength);

            var token = await storage.StageDeleteAsync(key);
            Assert.NotNull(token);
            Assert.Null(await storage.OpenReadAsync(key));
            Assert.True(await storage.RestoreStagedDeleteAsync(token!));
            await using (var restored = await storage.OpenReadAsync(key)) Assert.NotNull(restored);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Storage_validates_docx_and_rtf_containers()
    {
        var (storage, root) = CreateStorage();
        try
        {
            await using var docx = new MemoryStream();
            using (var zip = new ZipArchive(docx, ZipArchiveMode.Create, true))
            {
                zip.CreateEntry("[Content_Types].xml");
                zip.CreateEntry("word/document.xml");
            }
            docx.Position = 0;
            var key = await storage.SavePatientDocumentAsync(Guid.NewGuid(), Guid.NewGuid(), "تقرير.docx", "application/octet-stream", docx);
            await using (var storedDocx = await storage.OpenReadAsync(key)) Assert.NotNull(storedDocx);
            await Assert.ThrowsAsync<InvalidOperationException>(() => storage.SavePatientDocumentAsync(
                Guid.NewGuid(), Guid.NewGuid(), "bad.rtf", "application/rtf", new MemoryStream("plain text"u8.ToArray())));
            var rtf = await storage.SavePatientDocumentAsync(Guid.NewGuid(), Guid.NewGuid(), "عربي.rtf", "application/rtf", new MemoryStream("{\\rtf1 اختبار}"u8.ToArray()));
            await using (var storedRtf = await storage.OpenReadAsync(rtf)) Assert.NotNull(storedRtf);
        }
        finally { Directory.Delete(root, true); }
    }

    private static (LocalFileStorageService Storage, string Root) CreateStorage()
    {
        var root = Path.Combine(Path.GetTempPath(), $"clinic-media-{Guid.NewGuid():N}");
        var configuration = new ConfigurationManager { ["FileStorage:RootPath"] = root };
        return (new LocalFileStorageService(configuration), root);
    }


    [Fact]
    public async Task Delete_restores_file_when_database_delete_fails()
    {
        var (storage, root) = CreateStorage();
        try
        {
            var tenant = Guid.NewGuid(); var patient = Guid.NewGuid(); var id = Guid.NewGuid();
            var key = await storage.SavePatientDocumentAsync(tenant, patient, "x.pdf", "application/pdf", new MemoryStream("%PDF-1.4"u8.ToArray()));
            var repository = Substitute.For<IPatientDocumentRepository>();
            repository.GetByIdAsync(tenant, patient, id).Returns(new PatientDocument { Id = id, TenantId = tenant, PatientId = patient, FileUrl = key, FileName = "x.pdf", FileType = "application/pdf" });
            repository.DeleteAsync(tenant, patient, id).Returns<Task<bool>>(_ => throw new InvalidOperationException("db down"));
            var result = await new DeletePatientDocumentCommand.Handler(repository, storage).Handle(new(tenant, patient, id));
            Assert.False(result.Success);
            await using var restored = await storage.OpenReadAsync(key);
            Assert.NotNull(restored);
        }
        finally { Directory.Delete(root, true); }
    }
}
