using AutoMapper;
using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using FluentValidation;

namespace Clinic.Saas.Service.UseCases.Users.Commands;

public class CreateUserCommand
{
    public class Command
    {
        public Guid TenantId { get; set; }
        public CreateUserDto User { get; set; } = null!;
    }

    public class Handler
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordService _passwordService;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateUserDto> _validator;

        public Handler(
            IUserRepository userRepository,
            IPasswordService passwordService,
            IMapper mapper,
            IValidator<CreateUserDto> validator)
        {
            _userRepository = userRepository;
            _passwordService = passwordService;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<BaseResponse<UserDto>> Handle(Command command)
        {
            var validation = await _validator.ValidateAsync(command.User);
            if (!validation.IsValid)
            {
                return new BaseResponse<UserDto>
                {
                    Success = false,
                    Message = "بيانات المستخدم غير صحيحة",
                    Errors = validation.Errors.Select(x => x.ErrorMessage).ToList(),
                    StatusCode = 400
                };
            }

            if (await _userRepository.ExistsByEmailAsync(command.TenantId, command.User.Email))
            {
                return new BaseResponse<UserDto>
                {
                    Success = false,
                    Message = "البريد الإلكتروني مستخدم بالفعل",
                    StatusCode = 409
                };
            }

            var entity = new User
            {
                TenantId = command.TenantId,
                FullName = command.User.FullName,
                Email = command.User.Email,
                PasswordHash = _passwordService.HashPassword(command.User.Password),
                Role = command.User.Role,
                Phone = command.User.Phone,
                Specialty = command.User.Specialty,
                LicenseNumber = command.User.LicenseNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var created = await _userRepository.AddAsync(entity);
            return new BaseResponse<UserDto>
            {
                Success = true,
                Message = "تم إنشاء المستخدم بنجاح",
                Data = _mapper.Map<UserDto>(created),
                StatusCode = 201
            };
        }
    }
}
