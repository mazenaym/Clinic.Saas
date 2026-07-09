using System.Text.Json;
using Clinic.Saas.Service.DTOs;
using FluentValidation;

namespace Clinic.Saas.Service.Validators;

public class CreatePlatformPlanRequestValidator : AbstractValidator<CreatePlatformPlanRequest>
{
    public CreatePlatformPlanRequestValidator()
    {
        Include(new PlatformPlanRules<CreatePlatformPlanRequest>());
    }
}

public class UpdatePlatformPlanRequestValidator : AbstractValidator<UpdatePlatformPlanRequest>
{
    public UpdatePlatformPlanRequestValidator()
    {
        Include(new PlatformPlanRules<UpdatePlatformPlanRequest>());
    }
}

public class UpsertPlatformPlanDtoValidator : AbstractValidator<UpsertPlatformPlanDto>
{
    public UpsertPlatformPlanDtoValidator()
    {
        Include(new PlatformPlanRules<UpsertPlatformPlanDto>());
    }
}

public class UpdatePlatformPlanStatusRequestValidator : AbstractValidator<UpdatePlatformPlanStatusRequest>
{
    public UpdatePlatformPlanStatusRequestValidator()
    {
        RuleFor(x => x.IsActive).NotNull();
    }
}

internal class PlatformPlanRules<T> : AbstractValidator<T>
{
    public PlatformPlanRules()
    {
        RuleFor(x => GetString(x, "Name")).NotEmpty().MaximumLength(150).WithName("Name");
        RuleFor(x => GetString(x, "Code")).NotEmpty().MaximumLength(50).WithName("Code");
        RuleFor(x => GetDecimal(x, "Price")).GreaterThanOrEqualTo(0).WithName("Price");
        RuleFor(x => GetInt(x, "DurationDays")).GreaterThan(0).WithName("DurationDays");
        RuleFor(x => GetNullableInt(x, "MaxDoctors")).GreaterThanOrEqualTo(0).When(x => GetNullableInt(x, "MaxDoctors").HasValue).WithName("MaxDoctors");
        RuleFor(x => GetNullableInt(x, "MaxUsers")).GreaterThanOrEqualTo(0).When(x => GetNullableInt(x, "MaxUsers").HasValue).WithName("MaxUsers");
        RuleFor(x => GetNullableInt(x, "MaxPatients")).GreaterThanOrEqualTo(0).When(x => GetNullableInt(x, "MaxPatients").HasValue).WithName("MaxPatients");
        RuleFor(x => GetString(x, "FeaturesJson"))
            .Must(BeValidJson)
            .When(x => !string.IsNullOrWhiteSpace(GetString(x, "FeaturesJson")))
            .WithMessage("FeaturesJson must be valid JSON.");
    }

    private static string? GetString(T item, string propertyName) => typeof(T).GetProperty(propertyName)?.GetValue(item) as string;

    private static decimal GetDecimal(T item, string propertyName) => (decimal)(typeof(T).GetProperty(propertyName)?.GetValue(item) ?? 0m);

    private static int GetInt(T item, string propertyName) => (int)(typeof(T).GetProperty(propertyName)?.GetValue(item) ?? 0);

    private static int? GetNullableInt(T item, string propertyName) => typeof(T).GetProperty(propertyName)?.GetValue(item) as int?;

    private static bool BeValidJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        try
        {
            using var _ = JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
