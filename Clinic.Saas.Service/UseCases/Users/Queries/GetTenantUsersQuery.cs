using AutoMapper;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Users.Queries;

public class GetTenantUsersQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
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

        public async Task<BaseResponse<List<UserDto>>> Handle(Query query)
        {
            var users = await _userRepository.GetByTenantAsync(query.TenantId);
            return new BaseResponse<List<UserDto>>
            {
                Success = true,
                Data = _mapper.Map<List<UserDto>>(users),
                StatusCode = 200
            };
        }
    }
}
