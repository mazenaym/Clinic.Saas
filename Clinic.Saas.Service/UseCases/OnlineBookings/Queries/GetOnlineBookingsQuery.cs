using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.OnlineBookings.Queries;

public class GetOnlineBookingsQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
    }

    public class Handler
    {
        private readonly IOnlineBookingRepository _onlineBookingRepository;

        public Handler(IOnlineBookingRepository onlineBookingRepository)
        {
            _onlineBookingRepository = onlineBookingRepository;
        }

        public async Task<BaseResponse<List<OnlineBookingOperationDto>>> Handle(Query query)
        {
            var bookings = await _onlineBookingRepository.GetByTenantAsync(query.TenantId);
            return new BaseResponse<List<OnlineBookingOperationDto>>
            {
                Success = true,
                Data = bookings.Select(x => new OnlineBookingOperationDto
                {
                    Id = x.Id,
                    PatientName = x.PatientName,
                    PatientPhone = x.PatientPhone,
                    PatientEmail = x.PatientEmail,
                    RequestedDate = x.RequestedDate,
                    RequestedTime = x.RequestedTime,
                    DoctorId = x.DoctorId,
                    Complaint = x.Complaint,
                    Status = x.Status.ToString(),
                    ConfirmCode = x.ConfirmCode,
                    RejectReason = x.RejectReason,
                    CreatedAt = x.CreatedAt
                }).ToList(),
                Message = "OK",
                StatusCode = 200
            };
        }
    }
}
