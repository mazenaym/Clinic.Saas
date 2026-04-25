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
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender.ToString()));

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
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.Appointments, opt => opt.Ignore())
            .ForMember(dest => dest.Visits, opt => opt.Ignore())
            .ForMember(dest => dest.Prescriptions, opt => opt.Ignore())
            .ForMember(dest => dest.Payments, opt => opt.Ignore())
            .ForMember(dest => dest.Documents, opt => opt.Ignore());

        CreateMap<UpdatePatientDto, Patient>()
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.PatientCode, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.Appointments, opt => opt.Ignore())
            .ForMember(dest => dest.Visits, opt => opt.Ignore())
            .ForMember(dest => dest.Prescriptions, opt => opt.Ignore())
            .ForMember(dest => dest.Payments, opt => opt.Ignore())
            .ForMember(dest => dest.Documents, opt => opt.Ignore());

        CreateMap<UpdatePatientDto, CreatePatientDto>();

        CreateMap<Appointment, AppointmentDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.Source, opt => opt.MapFrom(src => src.Source.ToString()))
            .ForMember(dest => dest.PatientName, opt => opt.MapFrom(_ => string.Empty))
            .ForMember(dest => dest.PatientPhone, opt => opt.MapFrom(_ => string.Empty))
            .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(_ => string.Empty));

        CreateMap<Visit, VisitDto>()
            .ForMember(dest => dest.VisitType, opt => opt.MapFrom(src => src.VisitType.ToString()))
            .ForMember(dest => dest.VitalSigns, opt => opt.MapFrom(src =>
                string.IsNullOrWhiteSpace(src.VitalSigns)
                    ? null
                    : JsonSerializer.Deserialize<VitalSignsDto>(src.VitalSigns)))
            .ForMember(dest => dest.PatientName, opt => opt.MapFrom(_ => string.Empty))
            .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(_ => string.Empty));

        CreateMap<PrescriptionItem, PrescriptionItemDto>();

        CreateMap<Prescription, PrescriptionDto>()
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))
            .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(_ => string.Empty))
            .ForMember(dest => dest.PatientName, opt => opt.MapFrom(_ => string.Empty));

        CreateMap<Payment, PaymentDto>()
            .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.PaymentMethod.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.RemainingAmount, opt => opt.MapFrom(src => (src.TotalAmount + src.TaxAmount) - src.DiscountAmount - src.PaidAmount))
            .ForMember(dest => dest.PatientName, opt => opt.MapFrom(_ => string.Empty));
    }
}
