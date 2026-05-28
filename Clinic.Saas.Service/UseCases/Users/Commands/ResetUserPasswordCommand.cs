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
