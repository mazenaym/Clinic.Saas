using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.UseCases.Auth.Commands
{
    public class ChangePasswordCommand
    {
        public class Command
        {
            public Guid TenantId { get; set; }
            public Guid UserId { get; set; }
            public ChangePasswordDto Request { get; set; } = null!;
        }

        public class Handler
        {
            private readonly IUserRepository _userRepository;
            private readonly IPasswordService _passwordService;
            private readonly IValidator<ChangePasswordDto> _validator;

            public Handler(
                IUserRepository userRepository,
                IPasswordService passwordService,
                IValidator<ChangePasswordDto> validator)
            {
                _userRepository = userRepository;
                _passwordService = passwordService;
                _validator = validator;
            }

            public async Task<BaseResponse<bool>> Handle(Command command)
            {
                var validation = await _validator.ValidateAsync(command.Request);
                if (!validation.IsValid)
                {
                    return new BaseResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid password data.",
                        Errors = validation.Errors.Select(x => x.ErrorMessage).ToList(),
                        StatusCode = 400
                    };
                }

                var user = await _userRepository.GetActiveByIdAsync(command.TenantId, command.UserId);
                if (user is null)
                {
                    return new BaseResponse<bool>
                    {
                        Success = false,
                        Message = "User not found.",
                        StatusCode = 404
                    };
                }

                var currentPasswordIsValid = _passwordService.VerifyPassword(
                    user.PasswordHash,
                    command.Request.CurrentPassword);

                if (!currentPasswordIsValid)
                {
                    return new BaseResponse<bool>
                    {
                        Success = false,
                        Message = "Current password is incorrect.",
                        StatusCode = 400
                    };
                }

                var newPasswordHash = _passwordService.HashPassword(command.Request.NewPassword);

                var updated = await _userRepository.UpdatePasswordAsync(
                    command.TenantId,
                    command.UserId,
                    newPasswordHash);

                if (!updated)
                {
                    return new BaseResponse<bool>
                    {
                        Success = false,
                        Message = "Password was not updated.",
                        StatusCode = 409
                    };
                }

                return new BaseResponse<bool>
                {
                    Success = true,
                    Message = "Password changed successfully.",
                    Data = true,
                    StatusCode = 200
                };
            }
        }
       }
    }
