using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Auth.Commands;

public class LogoutCommand
{
        public class Command
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

        public async Task<BaseResponse<object>> Handle(Command command)
        {
            await _userRepository.UpdateRefreshTokenAsync(command.TenantId, command.UserId, null, null);
            return new BaseResponse<object>
            {
                Success = true,
                Message = "تم تسجيل الخروج",
                StatusCode = 200
            };
        }
    }
}
