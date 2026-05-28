using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Patients.Queries;

public class GetPatientChartQuery
{
    public class Query
    {
        public Guid TenantId { get; set; }
        public Guid PatientId { get; set; }
    }

    public class Handler
    {
        private readonly IPatientRepository _repository;

        public Handler(IPatientRepository repository)
        {
            _repository = repository;
        }

        public async Task<BaseResponse<PatientChartDto>> Handle(Query query)
        {
            var chart = await _repository.GetChartAsync(query.TenantId, query.PatientId);
            if (chart.Patient is null)
            {
                return new BaseResponse<PatientChartDto>
                {
                    Success = false,
                    Message = "Patient not found.",
                    StatusCode = 404
                };
            }

            var patient = chart.Patient;
            return new BaseResponse<PatientChartDto>
            {
                Success = true,
                Message = "OK",
                StatusCode = 200,
                Data = new PatientChartDto
                {
                    Patient = new PatientChartDemographicsDto
                    {
                        Id = patient.Id,
                        PatientCode = patient.PatientCode,
                        FullName = patient.FullName,
                        PhoneNumber = patient.PhoneNumber,
                        DateOfBirth = patient.DateOfBirth,
                        Gender = patient.Gender.ToString(),
                        BloodType = patient.BloodType,
                        Email = patient.Email,
                        Address = patient.Address,
                        InsuranceCompany = patient.InsuranceCompany,
                        CreatedAt = patient.CreatedAt,
                        RowVersion = patient.RowVersion.ToBase64RowVersion()
                    },
                    MedicalWarnings = new PatientChartMedicalWarningsDto
                    {
                        DrugAllergies = patient.DrugAllergies,
                        ChronicDiseases = patient.ChronicDiseases
                    },
                    RecentVisits = chart.RecentVisits.Select(visit => new PatientChartVisitDto
                    {
                        Id = visit.Id,
                        VisitDate = visit.VisitDate,
                        VisitType = visit.VisitType.ToString(),
                        ChiefComplaint = visit.ChiefComplaint,
                        Diagnosis = visit.Diagnosis,
                        DiagnosisCode = visit.DiagnosisCode,
                        DoctorName = visit.DoctorName,
                        RowVersion = visit.RowVersion.ToBase64RowVersion()
                    }).ToList(),
                    RecentPrescriptions = chart.RecentPrescriptions.Select(prescription => new PatientChartPrescriptionSummaryDto
                    {
                        Id = prescription.Id,
                        CreatedAt = prescription.CreatedAt,
                        DoctorName = prescription.DoctorName,
                        ItemCount = prescription.ItemCount,
                        ItemsSummary = prescription.ItemsSummary,
                        SentViaWhatsapp = prescription.SentViaWhatsapp,
                        RowVersion = prescription.RowVersion.ToBase64RowVersion()
                    }).ToList(),
                    RecentAppointments = chart.RecentAppointments.Select(appointment => new PatientChartAppointmentDto
                    {
                        Id = appointment.Id,
                        AppointmentDate = appointment.AppointmentDate,
                        StartTime = appointment.StartTime,
                        EndTime = appointment.EndTime,
                        Status = appointment.Status.ToString(),
                        Type = appointment.Type.ToString(),
                        DoctorName = appointment.DoctorName,
                        Notes = appointment.Notes,
                        RowVersion = appointment.RowVersion.ToBase64RowVersion()
                    }).ToList(),
                    PaymentSummary = new PatientChartPaymentSummaryDto
                    {
                        InvoiceCount = chart.PaymentSummary.InvoiceCount,
                        TotalPaid = chart.PaymentSummary.TotalPaid,
                        TotalOutstanding = chart.PaymentSummary.TotalOutstanding,
                        LastPaymentAt = chart.PaymentSummary.LastPaymentAt
                    },
                    Documents = chart.Documents.Select(document => new PatientChartDocumentDto
                    {
                        Id = document.Id,
                        VisitId = document.VisitId,
                        FileName = document.FileName,
                        FileSizeKb = document.FileSizeKb,
                        FileType = document.FileType,
                        DocumentType = document.DocumentType.ToString(),
                        Description = document.Description,
                        UploadedAt = document.UploadedAt,
                        RowVersion = document.RowVersion.ToBase64RowVersion()
                    }).ToList(),
                    Timeline = chart.Timeline.Select(item => new PatientTimelineItemDto
                    {
                        Type = item.Type,
                        Id = item.Id,
                        Date = item.Date,
                        Title = item.Title,
                        Details = item.Details
                    }).ToList()
                }
            };
        }
    }
}
