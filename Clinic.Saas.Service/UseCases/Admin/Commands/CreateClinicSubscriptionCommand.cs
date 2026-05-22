using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using FluentValidation;

namespace Clinic.Saas.Service.UseCases.Admin.Commands;

public class CreateClinicSubscriptionCommand
{
    public class Command
    {
        public Guid ClinicId { get; set; }
        public CreateSubscriptionDto Subscription { get; set; } = null!;
    }

    public class Handler
    {
        private readonly IPlatformAdminRepository _repository;
        private readonly IValidator<CreateSubscriptionDto> _validator;

        public Handler(IPlatformAdminRepository repository, IValidator<CreateSubscriptionDto> validator)
        {
            _repository = repository;
            _validator = validator;
        }

        public async Task<BaseResponse<Subscription>> Handle(Command command)
        {
            var validation = await _validator.ValidateAsync(command.Subscription);
            if (!validation.IsValid)
            {
                return new BaseResponse<Subscription>
                {
                    Success = false,
                    Message = "Subscription data is invalid",
                    Errors = validation.Errors.Select(x => x.ErrorMessage).ToList(),
                    StatusCode = 400
                };
            }

            var clinic = await _repository.GetClinicByIdAsync(command.ClinicId);
            if (clinic is null)
            {
                return new BaseResponse<Subscription>
                {
                    Success = false,
                    Message = "Clinic was not found",
                    StatusCode = 404
                };
            }

            var created = await _repository.AddSubscriptionAsync(new Subscription
            {
                TenantId = command.ClinicId,
                Plan = command.Subscription.Plan,
                StartDate = command.Subscription.StartDate,
                EndDate = command.Subscription.EndDate,
                AmountPaid = command.Subscription.AmountPaid,
                Status = command.Subscription.Status,
                PaymentRef = command.Subscription.PaymentRef,
                Notes = command.Subscription.Notes,
                CreatedAt = DateTime.UtcNow
            });

            return new BaseResponse<Subscription>
            {
                Success = true,
                Message = "Subscription added successfully",
                Data = created,
                StatusCode = 201
            };
        }
    }
}
