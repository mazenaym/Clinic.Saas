namespace Clinic.Saas.Service.DTOs;

public static class RowVersionDtoExtensions
{
    public static string? ToBase64RowVersion(this byte[]? rowVersion) =>
        rowVersion is { Length: > 0 } ? Convert.ToBase64String(rowVersion) : null;

    public static byte[] FromBase64RowVersion(this string? rowVersion)
    {
        if (string.IsNullOrWhiteSpace(rowVersion))
        {
            return [];
        }

        try
        {
            return Convert.FromBase64String(rowVersion);
        }
        catch (FormatException)
        {
            return [];
        }
    }
}
