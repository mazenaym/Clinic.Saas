using System.Net;
using System.Net.Http.Json;
using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Clinic.Saas.Tests;

public sealed class BootstrapSecurityTests
{
    [Fact]
    public async Task Disabled_bootstrap_is_rejected()
    {
        await using var factory = new Factory(false);
        var response = await factory.CreateClient().PostAsJsonAsync("/api/admin/bootstrap", new { });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Missing_and_wrong_setup_keys_are_rejected()
    {
        await using var factory = new Factory(true);
        var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/admin/bootstrap", new { })).StatusCode);
        client.DefaultRequestHeaders.Add("X-ClinicFlow-Setup-Key", "wrong-secret");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/admin/bootstrap", new { })).StatusCode);
    }

    [Fact]
    public async Task Correct_key_bootstraps_once_and_secret_is_never_returned()
    {
        await using var factory = new Factory(true);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-ClinicFlow-Setup-Key", Factory.Secret);
        var first = await client.PostAsJsonAsync("/api/admin/bootstrap", new { });
        var body = await first.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.DoesNotContain(Factory.Secret, body);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/admin/bootstrap", new { })).StatusCode);
    }

    private sealed class Factory(bool enabled) : WebApplicationFactory<Program>
    {
        public const string Secret = "test-only-setup-key";
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Bootstrap:Enabled"] = enabled.ToString(), ["Bootstrap:SetupKey"] = Secret }));
            builder.ConfigureServices(services => { services.RemoveAll<IPlatformClinicsFacade>(); services.AddSingleton<IPlatformClinicsFacade, FakeClinicsFacade>(); });
        }
    }

    private sealed class FakeClinicsFacade : IPlatformClinicsFacade
    {
        private bool _created;
        public Task<BaseResponse<AdminClinicDto>> BootstrapAsync(BootstrapSuperAdminDto dto)
        {
            if (_created) return Task.FromResult(new BaseResponse<AdminClinicDto> { Success = false, Message = "Super admin already exists", StatusCode = 409 });
            _created = true;
            return Task.FromResult(new BaseResponse<AdminClinicDto> { Success = true, Message = "created", Data = new AdminClinicDto(), StatusCode = 201 });
        }
        public Task<IReadOnlyList<AdminClinicDto>> GetAsync(PlatformClinicFilterDto filter) => throw new NotSupportedException();
        public Task<BaseResponse<AdminClinicDto>> GetByIdAsync(Guid id) => throw new NotSupportedException();
        public Task<BaseResponse<AdminClinicDto>> CreateWithInitialSubscriptionAsync(CreateClinicDto dto, Guid? planId, Guid? actingUserId) => throw new NotSupportedException();
        public Task<BaseResponse<AdminClinicDto>> CreateAsync(CreateClinicDto dto) => throw new NotSupportedException();
        public Task<BaseResponse<AdminClinicDto>> UpdateAsync(Guid id, UpdateClinicDto dto) => throw new NotSupportedException();
        public Task<BaseResponse<AdminClinicDto>> SetLegacyStatusAsync(Guid id, bool isActive) => throw new NotSupportedException();
        public Task<BaseResponse<AdminClinicDto>> CreateLegacySubscriptionAsync(Guid clinicId, CreateSubscriptionDto dto) => throw new NotSupportedException();
    }
}
