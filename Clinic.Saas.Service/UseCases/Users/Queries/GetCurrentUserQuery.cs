using AutoMapper;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;

namespace Clinic.Saas.Service.UseCases.Users.Queries;

public class GetCurrentUserQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
    }

    public class Handler
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ISubscriptionService _subscriptionService;

        public Handler(IUserRepository userRepository, IMapper mapper, ISubscriptionService subscriptionService)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _subscriptionService = subscriptionService;
        }

        public async Task<BaseResponse<UserDto>> Handle(Query query)
        {
            var user = await _userRepository.GetByIdAsync(query.TenantId, query.UserId);
            if (user is null)
            {
                return new BaseResponse<UserDto>
                {
                    Success = false,
                    Message = "المستخدم غير موجود",
                    StatusCode = 404
                };
            }

            var dto = _mapper.Map<UserDto>(user);
            var subscription = await _subscriptionService.GetSubscriptionStatusAsync(query.TenantId);
            dto.SubscriptionStatus = subscription.SubscriptionStatus?.ToString();
            dto.SubscriptionEndsAtUtc = subscription.SubscriptionEndsAtUtc;
            dto.IsInGracePeriod = subscription.IsInGracePeriod;
            dto.DaysRemaining = subscription.DaysRemaining;

            return new BaseResponse<UserDto>
            {
                Success = true,
                Data = dto,
                StatusCode = 200
            };
        }
    }
}
