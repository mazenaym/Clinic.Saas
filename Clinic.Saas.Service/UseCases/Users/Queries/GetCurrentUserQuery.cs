using AutoMapper;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Users.Queries;

public class GetCurrentUserQuery
{
    public class Query
    {
        public Guid UserId { get; set; }
    }

    public class Handler
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public Handler(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<BaseResponse<UserDto>> Handle(Query query)
        {
            var user = await _userRepository.GetByIdAsync(query.UserId);
            if (user is null)
            {
                return new BaseResponse<UserDto>
                {
                    Success = false,
                    Message = "المستخدم غير موجود",
                    StatusCode = 404
                };
            }

            return new BaseResponse<UserDto>
            {
                Success = true,
                Data = _mapper.Map<UserDto>(user),
                StatusCode = 200
            };
        }
    }
}
