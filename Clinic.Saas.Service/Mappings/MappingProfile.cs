using AutoMapper;
using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Service.DTOs;
using System.Text.Json;

namespace Clinic.Saas.Service.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Patient, PatientDto>()
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender.ToString()))
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion.ToBase64RowVersion()));

        CreateMap<Patient, PatientDetailsDto>()
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender.ToString()));

        CreateMap<CreatePatientDto, Patient>()
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.PatientCode, opt => opt.Ignore())
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore());

        CreateMap<UpdatePatientDto, Patient>()
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.PatientCode, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion.FromBase64RowVersion()));

        CreateMap<UpdatePatientDto, CreatePatientDto>();

        CreateMap<Appointment, AppointmentDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.Source, opt => opt.MapFrom(src => src.Source.ToString()))
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion.ToBase64RowVersion()));

        CreateMap<Visit, VisitDto>()
            .ForMember(dest => dest.VisitType, opt => opt.MapFrom(src => src.VisitType.ToString()))
            .ForMember(dest => dest.VitalSigns, opt => opt.MapFrom(src =>
                string.IsNullOrWhiteSpace(src.VitalSigns)
                    ? null
                    : JsonSerializer.Deserialize<VitalSignsDto>(src.VitalSigns)))
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion.ToBase64RowVersion()));

        CreateMap<PrescriptionItem, PrescriptionItemDto>();

        CreateMap<Prescription, PrescriptionDto>()
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion.ToBase64RowVersion()));

        CreateMap<Payment, PaymentDto>()
            .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.PaymentMethod.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.RemainingAmount, opt => opt.MapFrom(src => src.RemainingAmount))
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion.ToBase64RowVersion()));

        CreateMap<Payment, PaymentDetailsDto>()
            .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.PaymentMethod.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.RemainingAmount, opt => opt.MapFrom(src => src.RemainingAmount))
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion.ToBase64RowVersion()));

        CreateMap<PaymentItem, PaymentItemDto>()
            .ForMember(dest => dest.ServiceType, opt => opt.MapFrom(src => src.ServiceType.ToString()));

        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));

        CreateMap<Tenant, TenantDto>()
            .ForMember(dest => dest.Plan, opt => opt.MapFrom(src => src.Plan.ToString()));
    }
}
