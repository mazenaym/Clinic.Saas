using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;

namespace Clinic.Saas.Service.UseCases.Users.Commands;

public class ResetUserPasswordCommand
{
    public class Command
    {
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public ResetPasswordDto Password { get; set; } = null!;
        public UserRole CallerRole { get; set; }
    }

    public class Handler
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordService _passwordService;

        public Handler(IUserRepository userRepository, IPasswordService passwordService)
        {
            _userRepository = userRepository;
            _passwordService = passwordService;
        }

        public async Task<BaseResponse<bool>> Handle(Command command)
        {
            var target = await _userRepository.GetByIdAsync(command.TenantId, command.UserId);
            if (target is null)
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = "User not found.",
                    StatusCode = 404
                };
            }

            if (command.CallerRole == UserRole.Admin && target.Role >= UserRole.Admin)
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = "Cannot reset password for another admin.",
                    StatusCode = 403
                };
            }

            var hash = _passwordService.HashPassword(command.Password.NewPassword);
            var updated = await _userRepository.ResetPasswordAsync(command.TenantId, command.UserId, hash);
            if (!updated)
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
                Message = "Password reset successfully.",
                StatusCode = 200
            };
        }
    }
}
