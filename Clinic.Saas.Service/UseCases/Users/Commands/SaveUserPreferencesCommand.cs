using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Users.Commands;

public class SaveUserPreferencesCommand
{
    public class Command
    {
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public UserPreferencesDto Preferences { get; set; } = null!;
    }

    public class Handler
    {
        private readonly IUserRepository _userRepository;

        public Handler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<BaseResponse<UserPreferencesDto>> Handle(Command command)
        {
            var updated = await _userRepository.UpdatePreferencesAsync(
                command.TenantId,
                command.UserId,
                command.Preferences.AvatarUrl);

            if (!updated)
            {
                return new BaseResponse<UserPreferencesDto>
                {
                    Success = false,
                    Message = "User not found.",
                    StatusCode = 404
                };
            }

            var user = await _userRepository.GetByIdAsync(command.TenantId, command.UserId);
            return new BaseResponse<UserPreferencesDto>
            {
                Success = true,
                Data = new UserPreferencesDto
                {
                    AvatarUrl = user?.AvatarUrl ?? command.Preferences.AvatarUrl,
                    Language = command.Preferences.Language ?? "ar",
                    Theme = command.Preferences.Theme ?? "light"
                },
                Message = "OK",
                StatusCode = 200
            };
        }
    }
}
