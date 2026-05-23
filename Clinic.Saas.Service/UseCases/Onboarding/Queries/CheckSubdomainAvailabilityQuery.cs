using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;

namespace Clinic.Saas.Service.UseCases.Onboarding.Queries;

public class CheckSubdomainAvailabilityQuery
{
    public class Query
    {
        public string Subdomain { get; set; } = string.Empty;
    }

    public class Handler
    {
        private readonly IPlatformAdminRepository _repository;

        public Handler(IPlatformAdminRepository repository)
        {
            _repository = repository;
        }

        public async Task<BaseResponse<SubdomainAvailabilityDto>> Handle(Query query)
        {
            var subdomain = NormalizeSubdomain(query.Subdomain);
            if (string.IsNullOrWhiteSpace(subdomain))
            {
                return Availability(subdomain, false, "Subdomain is required");
            }

            if (!IsFormatValid(subdomain))
            {
                return Availability(subdomain, false, "Subdomain can contain letters, numbers, and hyphens only");
            }

            if (ReservedSubdomains.Contains(subdomain))
            {
                return Availability(subdomain, false, "Subdomain is reserved");
            }

            var exists = await _repository.SubdomainExistsAsync(subdomain);
            return Availability(subdomain, !exists, exists ? "Subdomain is already used" : null);
        }

        internal static string NormalizeSubdomain(string subdomain) => subdomain.Trim().ToLowerInvariant();

        internal static bool IsReserved(string subdomain) => ReservedSubdomains.Contains(NormalizeSubdomain(subdomain));

        internal static bool IsFormatValid(string subdomain) =>
            subdomain.Length is >= 3 and <= 100 &&
            subdomain.All(c => char.IsLetterOrDigit(c) || c == '-') &&
            !subdomain.StartsWith('-') &&
            !subdomain.EndsWith('-');

        private static BaseResponse<SubdomainAvailabilityDto> Availability(string subdomain, bool isAvailable, string? reason) =>
            new()
            {
                Success = true,
                Data = new SubdomainAvailabilityDto
                {
                    Subdomain = subdomain,
                    IsAvailable = isAvailable,
                    Reason = reason
                },
                StatusCode = 200
            };

        private static readonly HashSet<string> ReservedSubdomains = new(StringComparer.OrdinalIgnoreCase)
        {
            "admin",
            "api",
            "app",
            "auth",
            "billing",
            "clinicflow",
            "dashboard",
            "help",
            "mail",
            "root",
            "support",
            "system",
            "www"
        };
    }
}
