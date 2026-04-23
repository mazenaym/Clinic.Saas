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

            CreateMap<CreatePatientDto, Patient>();
            CreateMap<UpdatePatientDto, Patient>();

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
