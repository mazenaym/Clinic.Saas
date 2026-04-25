using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Appointments.Queries;

public class GetAppointmentAvailabilityQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
        public Guid DoctorId { get; set; }
        public DateTime Date { get; set; }
    }

    public class Handler
    {
        private readonly IAppointmentRepository _repository;

        public Handler(IAppointmentRepository repository)
        {
            _repository = repository;
        }

        public async Task<BaseResponse<List<TimeSlotDto>>> Handle(Query query)
        {
            var bookedSlots = (await _repository.GetBookedSlotsAsync(query.TenantId, query.DoctorId, query.Date)).ToList();
            var availableSlots = new List<TimeSlotDto>();
            var start = new TimeSpan(9, 0, 0);
            var end = new TimeSpan(17, 0, 0);
            var slotDuration = TimeSpan.FromMinutes(30);

            for (var current = start; current < end; current += slotDuration)
            {
                var slotEnd = current + slotDuration;
                var isBooked = bookedSlots.Any(x => x.StartTime < slotEnd && x.EndTime > current);
                if (!isBooked)
                {
                    availableSlots.Add(new TimeSlotDto
                    {
                        StartTime = current,
                        EndTime = slotEnd,
                        DisplayText = $"{current:hh\\:mm} - {slotEnd:hh\\:mm}"
                    });
                }
            }

            return new BaseResponse<List<TimeSlotDto>>
            {
                Success = true,
                Data = availableSlots,
                StatusCode = 200
            };
        }
    }
}
