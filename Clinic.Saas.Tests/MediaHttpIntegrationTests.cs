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
using SkiaSharp;

namespace Clinic.Saas.Tests;

public sealed class MediaHttpIntegrationTests : IClassFixture<MediaHttpIntegrationTests.Factory>
{
    private readonly Factory _factory;
    private readonly HttpClient _client;
    public MediaHttpIntegrationTests(Factory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
    }

    [Fact]
    public async Task Avatar_can_be_uploaded_replaced_fetched_and_deleted_without_orphans()
    {
        await _client.DeleteAsync("/api/media/me/avatar");
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/media/me/avatar")).StatusCode);
        var first = await Upload("/api/media/me/avatar", "صورة أولى.png", ImageBytes(SKColors.Blue));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstKey = _factory.User.AvatarUrl!;
        var stored = _factory.Services.GetRequiredService<IFileStorageService>();
        await using (var firstStored = await stored.OpenReadAsync(firstKey)) Assert.NotNull(firstStored);

        var get = await _client.GetAsync("/api/media/me/avatar");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal("image/webp", get.Content.Headers.ContentType?.MediaType);

        Assert.Equal(HttpStatusCode.OK, (await Upload("/api/media/me/avatar", "ثانية.jpg", ImageBytes(SKColors.Green))).StatusCode);
        Assert.NotEqual(firstKey, _factory.User.AvatarUrl);
        Assert.Null(await stored.OpenReadAsync(firstKey));

        Assert.Equal(HttpStatusCode.OK, (await _client.DeleteAsync("/api/media/me/avatar")).StatusCode);
        Assert.Null(_factory.User.AvatarUrl);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/media/me/avatar")).StatusCode);
    }

    [Fact]
    public async Task Invalid_avatar_is_rejected_and_database_failure_keeps_previous_avatar()
    {
        await _client.DeleteAsync("/api/media/me/avatar");
        using var bad = Form("not-image.png", "not image"u8.ToArray(), "image/png");
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsync("/api/media/me/avatar", bad)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Upload("/api/media/me/avatar", "valid.png", ImageBytes(SKColors.Red))).StatusCode);
        var key = _factory.User.AvatarUrl;
        _factory.FailUserUpdate = true;
        try
        {
            var failed = await Upload("/api/media/me/avatar", "new.png", ImageBytes(SKColors.Yellow));
            Assert.True(failed.StatusCode == HttpStatusCode.InternalServerError, $"{failed.StatusCode}: {await failed.Content.ReadAsStringAsync()}");
        }
        finally { _factory.FailUserUpdate = false; }
        Assert.Equal(key, _factory.User.AvatarUrl);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/media/me/avatar")).StatusCode);
    }

    [Fact]
    public async Task Tenant_logo_is_admin_only_and_isolated_to_current_tenant()
    {
        await _client.DeleteAsync("/api/media/tenant/logo");
        using var viewer = _factory.CreateClient();
        viewer.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        viewer.DefaultRequestHeaders.Add("X-Test-Role", "Doctor");
        using var deniedForm = Form("logo.png", ImageBytes(SKColors.Purple), "image/png");
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PostAsync("/api/media/tenant/logo", deniedForm)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await Upload("/api/media/tenant/logo", "شعار.png", ImageBytes(SKColors.Purple))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/media/tenant/logo")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.DeleteAsync("/api/media/tenant/logo")).StatusCode);
        Assert.Null(_factory.Tenant.LogoUrl);
    }

    private Task<HttpResponseMessage> Upload(string path, string name, byte[] bytes)
    {
        var form = Form(name, bytes, "image/png");
        return _client.PostAsync(path, form);
    }

    private static MultipartFormDataContent Form(string name, byte[] bytes, string type)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes); file.Headers.ContentType = MediaTypeHeaderValue.Parse(type);
        form.Add(file, "file", name); return form;
    }

    private static byte[] ImageBytes(SKColor color)
    {
        using var bitmap = new SKBitmap(100, 80); bitmap.Erase(color);
        using var image = SKImage.FromBitmap(bitmap); using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    public sealed class Factory : WebApplicationFactory<Program>
    {
        public Guid TenantId { get; } = Guid.NewGuid();
        public Guid UserId { get; } = Guid.NewGuid();
        public User User { get; private set; } = null!;
        public Tenant Tenant { get; private set; } = null!;
        public bool FailUserUpdate { get; set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                User = new User { Id = UserId, TenantId = TenantId, FullName = "Cairo Admin", Email = "admin@test.local" };
                Tenant = new Tenant { Id = TenantId, Name = "Cairo Clinic", Subdomain = "cairo", Email = "clinic@test.local" };
                var users = Substitute.For<IUserRepository>();
                users.GetByIdAsync(TenantId, UserId).Returns(_ => User);
                users.UpdatePreferencesAsync(TenantId, UserId, Arg.Any<string?>()).Returns(call =>
                {
                    if (FailUserUpdate) throw new InvalidOperationException("db failure");
                    User.AvatarUrl = call.ArgAt<string?>(2); return true;
                });
                users.ClearAvatarAsync(TenantId, UserId).Returns(_ => { User.AvatarUrl = null; return true; });
                var tenants = Substitute.For<ITenantRepository>();
                tenants.GetByIdAsync(TenantId).Returns(_ => Tenant);
                tenants.UpdateAsync(Arg.Any<Tenant>()).Returns(Task.CompletedTask);
                var current = Substitute.For<ICurrentUserService>();
                current.TenantId.Returns(TenantId); current.UserId.Returns(UserId); current.IsAuthenticated.Returns(true);
                var audit = Substitute.For<IAuditService>();

                services.AddAuthentication(options => { options.DefaultAuthenticateScheme = "Test"; options.DefaultChallengeScheme = "Test"; })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
                services.RemoveAll<IUserRepository>(); services.AddSingleton(users);
                services.RemoveAll<ITenantRepository>(); services.AddSingleton(tenants);
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
