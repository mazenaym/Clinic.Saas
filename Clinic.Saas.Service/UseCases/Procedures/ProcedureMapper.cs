using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Procedures;

internal static class ProcedureMapper
{
    public static ProcedureDto ToDto(Procedure procedure) => new()
    {
        Id = procedure.Id,
        CategoryId = procedure.CategoryId,
        CategoryName = procedure.CategoryName,
        Name = procedure.Name,
        Specialty = procedure.Specialty,
        DefaultPrice = procedure.DefaultPrice,
        IsActive = procedure.IsActive,
        CreatedAt = procedure.CreatedAt,
        UpdatedAt = procedure.UpdatedAt
    };
}
