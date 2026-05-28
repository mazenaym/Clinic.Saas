using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Users.Commands;

public class DeactivateUserCommand
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

        public async Task<BaseResponse<bool>> Handle(Command command)
        {
            var user = await _userRepository.GetByIdAsync(command.TenantId, command.UserId);
            if (user is null)
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = "User not found.",
                    StatusCode = 404
                };
            }

            if (user.Role == UserRole.Admin)
            {
                var activeAdmins = await _userRepository.CountActiveAdminsAsync(command.TenantId);
                if (activeAdmins <= 1)
                {
                    return new BaseResponse<bool>
                    {
                        Success = false,
                        Message = "Cannot deactivate the last active admin in the clinic.",
                        StatusCode = 409
                    };
                }
            }

            var deactivated = await _userRepository.DeactivateAsync(command.TenantId, command.UserId);
            if (!deactivated)
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = "User not found.",
                    StatusCode = 404
                };
            }

            return new BaseResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "User deactivated.",
                StatusCode = 200
            };
        }
    }
}
