using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Clinic.Saas.Tests;

public sealed class PatientDocumentHttpIntegrationTests : IClassFixture<PatientDocumentHttpIntegrationTests.Factory>
{
    private readonly Factory _factory;
    private readonly HttpClient _client;
    public PatientDocumentHttpIntegrationTests(Factory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
    }

    [Fact]
    public async Task Multipart_upload_download_range_and_cross_patient_are_enforced()
    {
        using var form = Form("ملف عربي (1).pdf", "%PDF-1.4\n%%EOF"u8.ToArray(), "application/pdf", "1");
        var upload = await _client.PostAsync($"/api/patient-documents?patientId={_factory.PatientId}", form);
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        Assert.NotNull(_factory.Document);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/patient-documents/{_factory.Document!.Id}/download?patientId={_factory.PatientId}");
        request.Headers.Range = new RangeHeaderValue(0, 3);
        var download = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.PartialContent, download.StatusCode);
        Assert.Equal("application/pdf", download.Content.Headers.ContentType?.MediaType);
        Assert.Equal("nosniff", download.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal(4, (await download.Content.ReadAsByteArrayAsync()).Length);

        var cross = await _client.GetAsync($"/api/patient-documents/{_factory.Document.Id}/download?patientId={Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, cross.StatusCode);
    }

    [Theory]
    [InlineData("fake.pdf", "not pdf", "application/pdf", "1")]
    [InlineData("empty.pdf", "", "application/pdf", "1")]
    [InlineData("valid.pdf", "%PDF-1.4", "application/pdf", "99")]
    [InlineData("bad.rtf", "plain text", "application/rtf", "1")]
    public async Task Invalid_multipart_uploads_are_rejected(string name, string content, string type, string documentType)
    {
        using var form = Form(name, System.Text.Encoding.UTF8.GetBytes(content), type, documentType);
        var response = await _client.PostAsync($"/api/patient-documents?patientId={_factory.PatientId}", form);
        Assert.True(response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.RequestEntityTooLarge, response.StatusCode.ToString());
    }

    [Fact]
    public async Task Upload_without_edit_permission_is_forbidden()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Viewer");
        using var form = Form("valid.pdf", "%PDF-1.4"u8.ToArray(), "application/pdf", "1");
        var response = await client.PostAsync($"/api/patient-documents?patientId={_factory.PatientId}", form);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static MultipartFormDataContent Form(string name, byte[] bytes, string contentType, string documentType)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        form.Add(file, "file", name);
        form.Add(new StringContent(documentType), "documentType");
        return form;
    }

    public sealed class Factory : WebApplicationFactory<Program>
    {
        public Guid TenantId { get; } = Guid.NewGuid();
        public Guid UserId { get; } = Guid.NewGuid();
        public Guid PatientId { get; } = Guid.NewGuid();
        public PatientDocument? Document { get; private set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

                var patients = Substitute.For<IPatientRepository>();
                patients.GetByIdAsync(TenantId, PatientId).Returns(new Patient { Id = PatientId, TenantId = TenantId, FullName = "Test" });
                var documents = Substitute.For<IPatientDocumentRepository>();
                documents.AddAsync(Arg.Do<PatientDocument>(x => Document = x)).Returns(Task.CompletedTask);
                documents.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(call =>
                {
                    var tenant = call.ArgAt<Guid>(0); var patient = call.ArgAt<Guid>(1); var id = call.ArgAt<Guid>(2);
                    return tenant == TenantId && patient == PatientId && Document?.Id == id ? Document : null;
                });
                documents.DeleteAsync(TenantId, PatientId, Arg.Any<Guid>()).Returns(true);
                var current = Substitute.For<ICurrentUserService>();
                current.TenantId.Returns(TenantId); current.UserId.Returns(UserId); current.IsAuthenticated.Returns(true);
                var audit = Substitute.For<IAuditService>();

                services.RemoveAll<IPatientRepository>(); services.AddSingleton(patients);
                services.RemoveAll<IPatientDocumentRepository>(); services.AddSingleton(documents);
                services.RemoveAll<ICurrentUserService>(); services.AddSingleton(current);
                services.RemoveAll<IAuditService>(); services.AddSingleton(audit);
            });
        }
    }

    private sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var role = Request.Headers["X-Test-Role"].FirstOrDefault() ?? "Admin";
            var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()), new Claim(ClaimTypes.Role, role)], Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}
