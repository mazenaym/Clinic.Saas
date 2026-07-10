using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Enums;
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
        private readonly IPlanService _plans;
        private readonly ISubscriptionService _subscriptions;
        private readonly IValidator<CreateSubscriptionDto> _validator;

        public Handler(
            IPlatformAdminRepository repository,
            IPlanService plans,
            ISubscriptionService subscriptions,
            IValidator<CreateSubscriptionDto> validator)
        {
            _repository = repository;
            _plans = plans;
            _subscriptions = subscriptions;
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

            var plan = (await _plans.GetPlansAsync(includeInactive: true))
                .FirstOrDefault(x => x.Code.Equals(MapPlatformPlanCode(command.Subscription.Plan), StringComparison.OrdinalIgnoreCase));
            if (plan is null)
            {
                return new BaseResponse<Subscription>
                {
                    Success = false,
                    Message = "Matching platform subscription plan was not found",
                    StatusCode = 404
                };
            }

            var renewed = await _subscriptions.RenewSubscriptionAsync(new RenewTenantSubscriptionRequest(
                command.ClinicId,
                plan.Id,
                command.Subscription.EndDate,
                command.Subscription.AmountPaid,
                DateTime.UtcNow,
                string.IsNullOrWhiteSpace(command.Subscription.PaymentRef) ? null : command.Subscription.PaymentRef,
                command.Subscription.Notes),
                renewedByUserId: null);

            if (renewed is null)
            {
                return new BaseResponse<Subscription>
                {
                    Success = false,
                    Message = "Clinic was not found",
                    StatusCode = 404
                };
            }

            var created = new Subscription
            {
                TenantId = command.ClinicId,
                Plan = command.Subscription.Plan,
                StartDate = renewed.StartsAtUtc,
                EndDate = renewed.EndsAtUtc,
                AmountPaid = renewed.ActualPaidAmount ?? command.Subscription.AmountPaid,
                Status = renewed.Status,
                PaymentRef = command.Subscription.PaymentRef,
                Notes = command.Subscription.Notes,
                CreatedAt = renewed.RenewedAtUtc ?? DateTime.UtcNow
            };

            return new BaseResponse<Subscription>
            {
                Success = true,
                Message = "Subscription added successfully",
                Data = created,
                StatusCode = 201
            };
        }

        private static string MapPlatformPlanCode(PlanType plan)
            => plan switch
            {
                PlanType.Starter => "BASIC_MONTHLY",
                PlanType.Professional => "PRO_MONTHLY",
                PlanType.Enterprise => "ANNUAL",
                _ => "BASIC_MONTHLY"
            };
    }
}
