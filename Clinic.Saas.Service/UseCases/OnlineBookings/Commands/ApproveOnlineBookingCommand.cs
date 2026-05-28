using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.OnlineBookings.Commands;

public class ApproveOnlineBookingCommand
{
    public class Command
    {
        public Guid TenantId { get; set; }
        public Guid BookingId { get; set; }
    }

    public class Handler
    {
        private readonly IOnlineBookingRepository _onlineBookingRepository;

        public Handler(IOnlineBookingRepository onlineBookingRepository)
        {
            _onlineBookingRepository = onlineBookingRepository;
        }

        public async Task<BaseResponse<bool>> Handle(Command command)
        {
            var updated = await _onlineBookingRepository.UpdateStatusAsync(
                command.TenantId,
                command.BookingId,
                OnlineBookingStatus.Confirmed);

            if (!updated)
            {
                return new BaseResponse<bool>
                {
                    Success = false,
                    Message = "Online booking not found.",
                    StatusCode = 404
                };
            }

            return new BaseResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Online booking approved.",
                StatusCode = 200
            };
        }
    }
}
