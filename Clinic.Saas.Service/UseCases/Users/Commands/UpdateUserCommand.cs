using AutoMapper;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Users.Commands;

public class UpdateUserCommand
{
    public class Command
    {
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public UpdateUserDto User { get; set; } = null!;
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

        public async Task<BaseResponse<UserDto>> Handle(Command command)
        {
            var existing = await _userRepository.GetByIdAsync(command.TenantId, command.UserId);
            if (existing is null)
            {
                return new BaseResponse<UserDto>
                {
                    Success = false,
                    Message = "User not found.",
                    StatusCode = 404
                };
            }

            if (await _userRepository.IsEmailTakenByAnotherUserAsync(command.TenantId, command.UserId, command.User.Email))
            {
                return new BaseResponse<UserDto>
                {
                    Success = false,
                    Message = "Email is already used in this clinic.",
                    StatusCode = 409
                };
            }

            existing.FullName = command.User.FullName;
            existing.Email = command.User.Email;
            existing.Role = command.User.Role;
            existing.Phone = command.User.Phone;
            existing.Specialty = command.User.Specialty;
            existing.LicenseNumber = command.User.LicenseNumber;

            var updated = await _userRepository.UpdateAdminUserAsync(command.TenantId, existing);
            if (!updated)
            {
                return new BaseResponse<UserDto>
                {
                    Success = false,
                    Message = "User not found.",
                    StatusCode = 404
                };
            }

            var user = await _userRepository.GetByIdAsync(command.TenantId, command.UserId);
            return new BaseResponse<UserDto>
            {
                Success = true,
                Data = _mapper.Map<UserDto>(user ?? existing),
                Message = "OK",
                StatusCode = 200
            };
        }
    }
}
