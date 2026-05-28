using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Users.Queries;

public class GetUserPreferencesQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
    }

    public class Handler
    {
        private readonly IUserRepository _userRepository;

        public Handler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<BaseResponse<UserPreferencesDto>> Handle(Query query)
        {
            var user = await _userRepository.GetByIdAsync(query.TenantId, query.UserId);
            if (user is null)
            {
                return new BaseResponse<UserPreferencesDto>
                {
                    Success = false,
                    Message = "User not found.",
                    StatusCode = 404
                };
            }

            return new BaseResponse<UserPreferencesDto>
            {
                Success = true,
                Data = new UserPreferencesDto
                {
                    AvatarUrl = user.AvatarUrl,
                    Language = "ar",
                    Theme = "light"
                },
                Message = "OK",
                StatusCode = 200
            };
        }
    }
}
