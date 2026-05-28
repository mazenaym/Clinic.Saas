using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Prescriptions.Commands;

public class SendPrescriptionWhatsappCommand
{
    public class Command
    {
        public Guid TenantId { get; set; }
        public Guid PrescriptionId { get; set; }
    }

    public class Handler
    {
        private readonly IPrescriptionRepository _prescriptionRepository;
        private readonly IClinicSettingsRepository _settingsRepository;

        public Handler(IPrescriptionRepository prescriptionRepository, IClinicSettingsRepository settingsRepository)
        {
            _prescriptionRepository = prescriptionRepository;
            _settingsRepository = settingsRepository;
        }

        public async Task<BaseResponse<object>> Handle(Command command)
        {
            var whatsappEnabled = await _settingsRepository.IsWhatsappEnabledAsync(command.TenantId);
            if (!whatsappEnabled)
            {
                return new BaseResponse<object>
                {
                    Success = false,
                    Message = "WhatsApp integration is disabled for this clinic.",
                    StatusCode = 409
                };
            }

            var prescription = await _prescriptionRepository.GetByIdAsync(command.TenantId, command.PrescriptionId);
            if (prescription is null)
            {
                return new BaseResponse<object>
                {
                    Success = false,
                    Message = "Prescription not found.",
                    StatusCode = 404
                };
            }

            await _prescriptionRepository.MarkSentViaWhatsappAsync(command.TenantId, command.PrescriptionId);

            return new BaseResponse<object>
            {
                Success = true,
                Message = "Prescription marked as sent via WhatsApp.",
                Data = true,
                StatusCode = 200
            };
        }
    }
}
