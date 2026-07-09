namespace Clinic.Saas.Service.Interfaces;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}

public interface IWhatsAppSender
{
    Task SendAsync(string to, string message, CancellationToken cancellationToken = default);
}
