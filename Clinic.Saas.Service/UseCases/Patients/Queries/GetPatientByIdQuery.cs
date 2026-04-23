using AutoMapper;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.UseCases.Patients.Queries
{
    public class GetPatientByIdQuery
    {
        public class Query
        {
            public Guid Id { get; set; }
        }

        public class Handler
        {
            private readonly IPatientRepository _repository;
            private readonly IMapper _mapper;

            public Handler(IPatientRepository repository, IMapper mapper)
            {
                _repository = repository;
                _mapper = mapper;
            }

            public async Task<BaseResponse<PatientDto>> Handle(Query query)
            {
                var patient = await _repository.GetByIdAsync(query.Id);

                if (patient == null)
                {
                    return new BaseResponse<PatientDto>
                    {
                        Success = false,
                        Message = "المريض غير موجود",
                        StatusCode = 404
                    };
                }

                var result = _mapper.Map<PatientDto>(patient);

                return new BaseResponse<PatientDto>
                {
                    Success = true,
                    Data = result,
                    StatusCode = 200
                };
            }
        }
    }
    }
