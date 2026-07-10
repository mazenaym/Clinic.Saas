using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Clinic.Saas.Tests;

public sealed class SwaggerIntegrationTests : IClassFixture<SwaggerIntegrationTests.Factory>
{
    private readonly HttpClient _client;

    public SwaggerIntegrationTests(Factory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Swagger_generates_without_route_conflicts_and_contains_focused_platform_routes()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"{response.StatusCode}: {body}");

        using var document = JsonDocument.Parse(body);
        var paths = document.RootElement.GetProperty("paths");
        string[] expected =
        [
            "/api/platform/dashboard/summary", "/api/platform/clinics", "/api/platform/plans",
            "/api/platform/subscriptions", "/api/platform/reports/platform",
            "/api/platform/settings/platform", "/api/platform/audit-logs"
        ];
        foreach (var path in expected) Assert.True(paths.TryGetProperty(path, out _), path);
    }

    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing");
    }
}
