using Clinic.Saas.Service.Interfaces;
using Microsoft.Extensions.Logging;

namespace Clinic.Saas.Infrastructure.Services;

public class NoopEmailSender : IEmailSender
{
    private readonly ILogger<NoopEmailSender> _logger;

    public NoopEmailSender(ILogger<NoopEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Development email hook: {To} {Subject}", to, subject);
        return Task.CompletedTask;
    }
}

public class NoopWhatsAppSender : IWhatsAppSender
{
    private readonly ILogger<NoopWhatsAppSender> _logger;

    public NoopWhatsAppSender(ILogger<NoopWhatsAppSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Development WhatsApp hook: {To}", to);
        return Task.CompletedTask;
    }
}
