using AutoMapper;
using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Service.DTOs;
using System;
using System.Collections.Generic;
using System.Text;


namespace Clinic.Saas.Service.Mappings
{
    public class MappingProfile: Profile
    {
        public MappingProfile()
        {
            // Patient Mappings
            CreateMap<Patient, PatientDto>()
                .ForMember(dest => dest.Gender,
                    opt => opt.MapFrom(src => src.Gender.ToString()));

            CreateMap<Patient, PatientDetailsDto>()
                .ForMember(dest => dest.Gender,
                    opt => opt.MapFrom(src => src.Gender.ToString()));

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

            // Appointment Mappings
            CreateMap<Appointment, AppointmentDto>()
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.Type,
                    opt => opt.MapFrom(src => src.Type.ToString()))
                .ForMember(dest => dest.Source,
                    opt => opt.MapFrom(src => src.Source.ToString()));

            // ... باقي الـ Mappings
        }
    }
}
