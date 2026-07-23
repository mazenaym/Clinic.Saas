using AutoMapper;
using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.DTOs;
using Clinic.Saas.Service.Interfaces;
using Clinic.Saas.Service.Mappings;
using Clinic.Saas.Service.UseCases.Appointments.Commands;
using Clinic.Saas.Service.UseCases.Appointments.Queries;
using Clinic.Saas.Service.UseCases.PatientDocuments.Commands;
using Clinic.Saas.Service.UseCases.PatientDocuments.Queries;
using Clinic.Saas.Service.UseCases.Patients.Commands;
using Clinic.Saas.Service.UseCases.Patients.Queries;
using Clinic.Saas.Service.UseCases.Invoices.Commands;
using Clinic.Saas.Service.UseCases.Invoices.Queries;
using Clinic.Saas.Service.UseCases.Prescriptions.Commands;
using Clinic.Saas.Service.UseCases.Prescriptions.Queries;
using Clinic.Saas.Service.UseCases.Visits.Commands;
using Clinic.Saas.Service.UseCases.Visits.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Clinic.Saas.Tests;

public class TenantIsolationUseCaseTests
{
    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid UserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddAutoMapper(cfg => { }, typeof(MappingProfile).Assembly);
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    [Fact]
    public async Task Patients_read_and_delete_are_tenant_scoped()
    {
        var patientId = Guid.NewGuid();
        var repository = new FakePatientRepository();
        repository.Seed(new Patient
        {
            Id = patientId,
            TenantId = TenantB,
            PatientCode = "PB-001",
            FullName = "Tenant B Patient",
            PhoneNumber = "01000000000",
            Gender = Gender.Male,
            IsActive = true
        });

        var read = await new GetPatientByIdQuery.Handler(repository, CreateMapper()).Handle(new GetPatientByIdQuery.Query
        {
            TenantId = TenantA,
            Id = patientId
        });

        var delete = await new DeletePatientCommand.Handler(repository).Handle(new DeletePatientCommand.Command
        {
            TenantId = TenantA,
            Id = patientId
        });

        Assert.Equal(404, read.StatusCode);
        Assert.False(read.Success);
        Assert.Equal(404, delete.StatusCode);
        Assert.False(delete.Success);
        Assert.False(repository.Find(TenantB, patientId)!.IsDeleted);
    }

    [Fact]
    public async Task Appointments_range_and_status_update_are_tenant_scoped()
    {
        var appointmentId = Guid.NewGuid();
        var repository = new FakeAppointmentRepository();
        repository.Seed(new Appointment
        {
            Id = appointmentId,
            TenantId = TenantB,
            PatientId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            AppointmentDate = new DateTime(2026, 6, 1),
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(9, 30, 0),
            Status = AppointmentStatus.Scheduled,
            Type = AppointmentType.New,
            Source = AppointmentSource.Reception
        });

        var range = await new GetAppointmentRangeQuery.Handler(repository, CreateMapper()).Handle(new GetAppointmentRangeQuery.Query
        {
            TenantId = TenantA,
            From = new DateTime(2026, 6, 1),
            To = new DateTime(2026, 6, 2)
        });

        var update = await new UpdateAppointmentStatusCommand.Handler(repository).Handle(new UpdateAppointmentStatusCommand.Command
        {
            TenantId = TenantA,
            Request = new UpdateAppointmentStatusDto
            {
                Id = appointmentId,
                Status = AppointmentStatus.Confirmed
            }
        });

        Assert.True(range.Success);
        Assert.Empty(range.Data!);
        Assert.Equal(404, update.StatusCode);
        Assert.False(update.Success);
        Assert.Equal(AppointmentStatus.Scheduled, repository.Find(TenantB, appointmentId)!.Status);
    }

    [Fact]
    public async Task Visits_read_and_finalize_are_tenant_scoped()
    {
        var visitId = Guid.NewGuid();
        var repository = new FakeVisitRepository();
        repository.Seed(new Visit
        {
            Id = visitId,
            TenantId = TenantB,
            PatientId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            VisitDate = new DateTime(2026, 6, 1),
            VisitType = VisitType.New,
            ChiefComplaint = "Headache"
        });

        var read = await new GetVisitByIdQuery.Handler(repository, CreateMapper()).Handle(new GetVisitByIdQuery.Query
        {
            TenantId = TenantA,
            Id = visitId
        });

        var finalize = await new FinalizeVisitCommand.Handler(repository).Handle(new FinalizeVisitCommand.Command
        {
            TenantId = TenantA,
            VisitId = visitId,
            FinalizedByUserId = UserId
        });

        Assert.Equal(404, read.StatusCode);
        Assert.False(read.Success);
        Assert.Equal(404, finalize.StatusCode);
        Assert.False(finalize.Success);
        Assert.Null(repository.Find(TenantB, visitId)!.FinalizedAt);
    }

    [Fact]
    public async Task Prescriptions_read_and_mark_sent_are_tenant_scoped()
    {
        var prescriptionId = Guid.NewGuid();
        var repository = new FakePrescriptionRepository();
        repository.Seed(new Prescription
        {
            Id = prescriptionId,
            TenantId = TenantB,
            PatientId = Guid.NewGuid(),
            DoctorId = Guid.NewGuid(),
            VisitId = Guid.NewGuid(),
            IsActive = true
        });

        var read = await new GetPrescriptionByIdQuery.Handler(repository, CreateMapper()).Handle(new GetPrescriptionByIdQuery.Query
        {
            TenantId = TenantA,
            Id = prescriptionId
        });

        var send = await new SendPrescriptionWhatsappCommand.Handler(repository, new FakeClinicSettingsRepository()).Handle(
            new SendPrescriptionWhatsappCommand.Command
            {
                TenantId = TenantA,
                PrescriptionId = prescriptionId
            });

        Assert.Equal(404, read.StatusCode);
        Assert.False(read.Success);
        Assert.Equal(404, send.StatusCode);
        Assert.False(send.Success);
        Assert.False(repository.Find(TenantB, prescriptionId)!.SentViaWhatsapp);
    }

    [Fact]
    public async Task Payments_read_and_refund_are_tenant_scoped()
    {
        var invoiceId = Guid.NewGuid();
        var repository = new FakeInvoiceRepository();
        repository.Seed(new Invoice
        {
            Id = invoiceId,
            TenantId = TenantB,
            VisitId = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            InvoiceNumber = "INV-B-001",
            Subtotal = 200,
            GrandTotal = 200,
            PaidAmount = 200,
            RemainingAmount = 0,
            Status = InvoiceStatus.Paid
        });

        var read = await new GetInvoiceByIdQuery.Handler(repository).Handle(new GetInvoiceByIdQuery.Query
        {
            TenantId = TenantA,
            InvoiceId = invoiceId
        });

        var refund = await new RefundInvoiceCommand.Handler(repository).Handle(new RefundInvoiceCommand.Command
        {
            TenantId = TenantA,
            InvoiceId = invoiceId,
            Refund = new RefundInvoiceDto { Reason = "Wrong tenant attempt" }
        });

        Assert.Equal(404, read.StatusCode);
        Assert.False(read.Success);
        Assert.Equal(404, refund.StatusCode);
        Assert.False(refund.Success);
        Assert.Equal(InvoiceStatus.Paid, repository.Find(TenantB, invoiceId)!.Status);
    }

    [Fact]
    public async Task Patient_documents_download_and_upload_are_tenant_scoped()
    {
        var patientId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var patientRepository = new FakePatientRepository();
        var documentRepository = new FakePatientDocumentRepository();
        var fileStorage = new FakeFileStorageService();

        patientRepository.Seed(new Patient
        {
            Id = patientId,
            TenantId = TenantB,
            PatientCode = "PB-002",
            FullName = "Tenant B Document Patient",
            PhoneNumber = "01000000001",
            Gender = Gender.Female,
            IsActive = true
        });
        documentRepository.Seed(new PatientDocument
        {
            Id = documentId,
            TenantId = TenantB,
            PatientId = patientId,
            FileName = "lab.pdf",
            FileUrl = "tenant-b/lab.pdf",
            FileType = "application/pdf",
            DocumentType = DocumentType.LabResult,
            UploadedAt = DateTime.UtcNow
        });

        var download = await new DownloadPatientDocumentQuery.Handler(documentRepository, fileStorage).Handle(
            new DownloadPatientDocumentQuery.Query
            {
                TenantId = TenantA,
                PatientId = patientId,
                DocumentId = documentId
            });

        var upload = await new UploadPatientDocumentCommand.Handler(patientRepository, documentRepository, fileStorage).Handle(
            new UploadPatientDocumentCommand.Command
            {
                TenantId = TenantA,
                UserId = UserId,
                PatientId = patientId,
                FileName = "new.pdf",
                ContentType = "application/pdf",
                FileLength = 1024,
                FileStream = new MemoryStream([1, 2, 3]),
                DocumentType = (short)DocumentType.Other
            });

        Assert.Equal(404, download.StatusCode);
        Assert.False(download.Success);
        Assert.Equal(404, upload.StatusCode);
        Assert.False(upload.Success);
        Assert.Equal(0, fileStorage.OpenReadCalls);
        Assert.Equal(0, fileStorage.SaveCalls);
        Assert.Single(documentRepository.Documents);
    }

    private sealed class FakePatientRepository : IPatientRepository
    {
        private readonly List<Patient> _patients = [];

        public void Seed(Patient patient) => _patients.Add(patient);

        public Patient? Find(Guid tenantId, Guid id) => _patients.SingleOrDefault(x => x.TenantId == tenantId && x.Id == id);

        public Task<Patient?> GetByIdAsync(Guid tenantId, Guid id) =>
            Task.FromResult(_patients.SingleOrDefault(x => x.TenantId == tenantId && x.Id == id && !x.IsDeleted));

        public Task<IEnumerable<Patient>> GetAllAsync(Guid tenantId) =>
            Task.FromResult(_patients.Where(x => x.TenantId == tenantId && !x.IsDeleted).AsEnumerable());

        public Task UpdateAsync(Guid tenantId, Patient entity)
        {
            var existing = Find(tenantId, entity.Id);
            if (existing is not null)
            {
                existing.FullName = entity.FullName;
                existing.PhoneNumber = entity.PhoneNumber;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid tenantId, Guid id, byte[] rowVersion)
        {
            var existing = Find(tenantId, id);
            if (existing is not null)
            {
                existing.IsDeleted = true;
            }

            return Task.CompletedTask;
        }

        public Task<Patient?> GetByPhoneAsync(Guid tenantId, string phone) =>
            Task.FromResult(_patients.SingleOrDefault(x => x.TenantId == tenantId && x.PhoneNumber == phone));

        public Task<IEnumerable<Patient>> SearchAsync(Guid tenantId, string searchTerm) =>
            Task.FromResult(_patients.Where(x => x.TenantId == tenantId && x.FullName.Contains(searchTerm)).AsEnumerable());

        public Task<IEnumerable<PatientTimelineRow>> GetTimelineAsync(Guid tenantId, Guid patientId) =>
            Task.FromResult(Enumerable.Empty<PatientTimelineRow>());

        public Task<PatientChartData> GetChartAsync(Guid tenantId, Guid patientId) =>
            Task.FromResult(new PatientChartData());

        public Task<IEnumerable<PatientDuplicateRow>> FindDuplicatesAsync(Guid tenantId, string? phone, string? nationalId) =>
            Task.FromResult(Enumerable.Empty<PatientDuplicateRow>());

        public Task<IEnumerable<PatientExportRow>> GetExportRowsAsync(Guid tenantId) =>
            Task.FromResult(Enumerable.Empty<PatientExportRow>());

        public Task<bool> ExistsAsync(Guid tenantId, string phone) =>
            Task.FromResult(_patients.Any(x => x.TenantId == tenantId && x.PhoneNumber == phone));

        public Task<string> GenerateNextPatientCodeAsync(Guid tenantId) => Task.FromResult("P-TEST");

        public Task<Patient> AddAsync(Patient entity)
        {
            _patients.Add(entity);
            return Task.FromResult(entity);
        }

        public Task<Patient?> GetByIdAsync(Guid id) => Task.FromResult(_patients.SingleOrDefault(x => x.Id == id));
        public Task<IEnumerable<Patient>> GetAllAsync() => Task.FromResult(_patients.AsEnumerable());
        public Task UpdateAsync(Patient entity) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
    }

    private sealed class FakeAppointmentRepository : IAppointmentRepository
    {
        private readonly List<Appointment> _appointments = [];

        public void Seed(Appointment appointment) => _appointments.Add(appointment);
        public Appointment? Find(Guid tenantId, Guid id) => _appointments.SingleOrDefault(x => x.TenantId == tenantId && x.Id == id);

        public Task<Appointment?> GetByIdAsync(Guid tenantId, Guid id) =>
            Task.FromResult(_appointments.SingleOrDefault(x => x.TenantId == tenantId && x.Id == id && !x.IsDeleted));

        public Task<IEnumerable<Appointment>> GetAllAsync(Guid tenantId) =>
            Task.FromResult(_appointments.Where(x => x.TenantId == tenantId && !x.IsDeleted).AsEnumerable());

        public Task UpdateAsync(Guid tenantId, Appointment entity)
        {
            var existing = Find(tenantId, entity.Id);
            if (existing is not null)
            {
                existing.AppointmentDate = entity.AppointmentDate;
                existing.StartTime = entity.StartTime;
                existing.EndTime = entity.EndTime;
                existing.Status = entity.Status;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid tenantId, Guid id, byte[] rowVersion)
        {
            var existing = Find(tenantId, id);
            if (existing is not null)
            {
                existing.IsDeleted = true;
            }

            return Task.CompletedTask;
        }

        public Task<bool> HasConflictAsync(Guid tenantId, Guid doctorId, DateTime appointmentDate, TimeSpan startTime, TimeSpan endTime, Guid? excludeId = null) =>
            Task.FromResult(false);

        public Task<IEnumerable<Appointment>> GetByDateAsync(Guid tenantId, DateTime appointmentDate) =>
            Task.FromResult(_appointments.Where(x => x.TenantId == tenantId && x.AppointmentDate.Date == appointmentDate.Date).AsEnumerable());

        public Task<IEnumerable<Appointment>> GetByDateRangeAsync(Guid tenantId, DateTime from, DateTime to) =>
            Task.FromResult(_appointments.Where(x => x.TenantId == tenantId && x.AppointmentDate.Date >= from.Date && x.AppointmentDate.Date <= to.Date).AsEnumerable());

        public Task<IEnumerable<AppointmentCancellationReportRow>> GetCancellationReportAsync(Guid tenantId, DateTime from, DateTime to) =>
            Task.FromResult(Enumerable.Empty<AppointmentCancellationReportRow>());

        public Task<IEnumerable<TimeSlot>> GetBookedSlotsAsync(Guid tenantId, Guid doctorId, DateTime appointmentDate) =>
            Task.FromResult(Enumerable.Empty<TimeSlot>());

        public Task<bool> UpdateStatusAsync(Guid tenantId, Guid id, AppointmentStatus status, string? cancelReason, byte[] rowVersion)
        {
            var existing = Find(tenantId, id);
            if (existing is null)
            {
                return Task.FromResult(false);
            }

            existing.Status = status;
            existing.CancelReason = cancelReason;
            return Task.FromResult(true);
        }

        public Task<Appointment> AddAsync(Appointment entity)
        {
            _appointments.Add(entity);
            return Task.FromResult(entity);
        }

        public Task<Appointment?> GetByIdAsync(Guid id) => Task.FromResult(_appointments.SingleOrDefault(x => x.Id == id));
        public Task<IEnumerable<Appointment>> GetAllAsync() => Task.FromResult(_appointments.AsEnumerable());
        public Task UpdateAsync(Appointment entity) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
    }

    private sealed class FakeVisitRepository : IVisitRepository
    {
        private readonly List<Visit> _visits = [];

        public void Seed(Visit visit) => _visits.Add(visit);
        public Visit? Find(Guid tenantId, Guid id) => _visits.SingleOrDefault(x => x.TenantId == tenantId && x.Id == id);

        public Task<Visit?> GetByIdAsync(Guid tenantId, Guid id) =>
            Task.FromResult(_visits.SingleOrDefault(x => x.TenantId == tenantId && x.Id == id && !x.IsDeleted));

        public Task<IEnumerable<Visit>> GetAllAsync(Guid tenantId) =>
            Task.FromResult(_visits.Where(x => x.TenantId == tenantId && !x.IsDeleted).AsEnumerable());

        public Task UpdateAsync(Guid tenantId, Visit entity) => Task.CompletedTask;

        public Task DeleteAsync(Guid tenantId, Guid id, byte[] rowVersion)
        {
            var existing = Find(tenantId, id);
            if (existing is not null)
            {
                existing.IsDeleted = true;
            }

            return Task.CompletedTask;
        }

        public Task<IEnumerable<Visit>> GetByPatientIdAsync(Guid tenantId, Guid patientId) =>
            Task.FromResult(_visits.Where(x => x.TenantId == tenantId && x.PatientId == patientId).AsEnumerable());

        public Task<int> UpdateClinicalDetailsAsync(Guid tenantId, Guid id, Visit entity) =>
            Task.FromResult(Find(tenantId, id) is null ? 0 : 1);

        public Task<int> FinalizeAsync(Guid tenantId, Guid id, Guid finalizedByUserId, byte[] rowVersion)
        {
            var existing = Find(tenantId, id);
            if (existing is null)
            {
                return Task.FromResult(0);
            }

            existing.FinalizedAt = DateTime.UtcNow;
            existing.FinalizedBy = finalizedByUserId;
            return Task.FromResult(1);
        }

        public Task<int> CountByDateAsync(Guid tenantId, DateTime date) =>
            Task.FromResult(_visits.Count(x => x.TenantId == tenantId && x.VisitDate.Date == date.Date));

        public Task<Visit> AddAsync(Visit entity)
        {
            _visits.Add(entity);
            return Task.FromResult(entity);
        }

        public Task<Visit?> GetByIdAsync(Guid id) => Task.FromResult(_visits.SingleOrDefault(x => x.Id == id));
        public Task<IEnumerable<Visit>> GetAllAsync() => Task.FromResult(_visits.AsEnumerable());
        public Task UpdateAsync(Visit entity) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
    }

    private sealed class FakePrescriptionRepository : IPrescriptionRepository
    {
        private readonly List<Prescription> _prescriptions = [];

        public void Seed(Prescription prescription) => _prescriptions.Add(prescription);
        public Prescription? Find(Guid tenantId, Guid id) => _prescriptions.SingleOrDefault(x => x.TenantId == tenantId && x.Id == id);

        public Task<Prescription?> GetByIdAsync(Guid tenantId, Guid id) =>
            Task.FromResult(_prescriptions.SingleOrDefault(x => x.TenantId == tenantId && x.Id == id && x.IsActive));

        public Task UpdateAsync(Guid tenantId, Prescription entity) => Task.CompletedTask;

        public Task DeleteAsync(Guid tenantId, Guid id, byte[] rowVersion)
        {
            var existing = Find(tenantId, id);
            if (existing is not null)
            {
                existing.IsActive = false;
            }

            return Task.CompletedTask;
        }

        public Task<IEnumerable<Prescription>> GetByPatientIdAsync(Guid tenantId, Guid patientId) =>
            Task.FromResult(_prescriptions.Where(x => x.TenantId == tenantId && x.PatientId == patientId).AsEnumerable());

        public Task<int> MarkSentViaWhatsappAsync(Guid tenantId, Guid id, byte[] rowVersion)
        {
            var existing = Find(tenantId, id);
            if (existing is null)
            {
                return Task.FromResult(0);
            }

            existing.SentViaWhatsapp = true;
            return Task.FromResult(1);
        }

        public Task<Prescription> AddAsync(Prescription entity)
        {
            _prescriptions.Add(entity);
            return Task.FromResult(entity);
        }

        public Task<Prescription?> GetByIdAsync(Guid id) => Task.FromResult(_prescriptions.SingleOrDefault(x => x.Id == id));
        public Task<IEnumerable<Prescription>> GetAllAsync() => Task.FromResult(_prescriptions.AsEnumerable());
        public Task UpdateAsync(Prescription entity) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
    }

    private sealed class FakeInvoiceRepository : IInvoiceRepository
    {
        private readonly List<Invoice> _invoices = [];

        public void Seed(Invoice invoice) => _invoices.Add(invoice);
        public Invoice? Find(Guid tenantId, Guid id) => _invoices.SingleOrDefault(x => x.TenantId == tenantId && x.Id == id);

        public Task<Invoice?> GetByIdAsync(Guid tenantId, Guid id) =>
            Task.FromResult(_invoices.SingleOrDefault(x => x.TenantId == tenantId && x.Id == id));

        public Task<IEnumerable<Invoice>> GetByPatientAsync(Guid tenantId, Guid patientId) =>
            Task.FromResult(_invoices.Where(x => x.TenantId == tenantId && x.PatientId == patientId).AsEnumerable());

        public Task UpdateAsync(Guid tenantId, Invoice entity) => Task.CompletedTask;
        public Task<bool> UpdateWithItemsAsync(Guid tenantId, Invoice entity) => Task.FromResult(Find(tenantId, entity.Id) is not null);

        public Task<bool> RefundAsync(Guid tenantId, Guid id, string? reason, byte[] rowVersion)
        {
            var existing = Find(tenantId, id);
            if (existing is null)
            {
                return Task.FromResult(false);
            }

            existing.Status = InvoiceStatus.Refunded;
            existing.Notes = reason;
            return Task.FromResult(true);
        }

        public Task DeleteAsync(Guid tenantId, Guid id, byte[] rowVersion) => Task.CompletedTask;
        public Task<string> GenerateInvoiceNumberAsync(Guid tenantId, DateTime createdAt) => Task.FromResult("INV-TEST");
        public Task<IEnumerable<Invoice>> GetByDateAsync(Guid tenantId, DateTime date) =>
            Task.FromResult(_invoices.Where(x => x.TenantId == tenantId && x.CreatedAt.Date == date.Date).AsEnumerable());

        public Task<IEnumerable<InvoiceDebtRow>> GetDebtTrackingAsync(Guid tenantId) =>
            Task.FromResult(Enumerable.Empty<InvoiceDebtRow>());

        public Task<IEnumerable<MonthlyRevenueRow>> GetMonthlyRevenueAsync(Guid tenantId, DateTime start, DateTime end) =>
            Task.FromResult(Enumerable.Empty<MonthlyRevenueRow>());

        public Task<IEnumerable<DailyPaymentMethodTotal>> GetDailyPaymentMethodTotalsAsync(Guid tenantId, DateTime date) =>
            Task.FromResult(Enumerable.Empty<DailyPaymentMethodTotal>());

        public Task<Invoice> AddAsync(Invoice entity)
        {
            _invoices.Add(entity);
            return Task.FromResult(entity);
        }

        public Task<Invoice?> AddPaymentAsync(Guid tenantId, Guid invoiceId, InvoicePayment payment) => Task.FromResult<Invoice?>(null);
        public Task<PatientFinancialLedgerData> GetPatientLedgerAsync(Guid tenantId, Guid patientId) => Task.FromResult(new PatientFinancialLedgerData());
        public Task<FinancialDuesReportData> GetFinancialDuesAsync(Guid tenantId, DateTime? from, DateTime? toExclusive, Guid? doctorId) => Task.FromResult(new FinancialDuesReportData());
    }

    private sealed class FakePatientDocumentRepository : IPatientDocumentRepository
    {
        public List<PatientDocument> Documents { get; } = [];

        public void Seed(PatientDocument document) => Documents.Add(document);

        public Task AddAsync(PatientDocument document)
        {
            Documents.Add(document);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<PatientDocument>> GetByPatientAsync(Guid tenantId, Guid patientId) =>
            Task.FromResult(Documents.Where(x => x.TenantId == tenantId && x.PatientId == patientId).AsEnumerable());

        public Task<PatientDocument?> GetByIdAsync(Guid tenantId, Guid patientId, Guid documentId) =>
            Task.FromResult(Documents.SingleOrDefault(x =>
                x.TenantId == tenantId &&
                x.PatientId == patientId &&
                x.Id == documentId));
    }

    private sealed class FakeClinicSettingsRepository : IClinicSettingsRepository
    {
        public Task<bool> IsWhatsappEnabledAsync(Guid tenantId) => Task.FromResult(true);
    }

    private sealed class FakeFileStorageService : IFileStorageService
    {
        public int SaveCalls { get; private set; }
        public int OpenReadCalls { get; private set; }

        public Task<string> SavePatientDocumentAsync(Guid tenantId, Guid patientId, string originalFileName, string contentType, Stream fileStream, CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult($"{tenantId}/{patientId}/{Path.GetFileName(originalFileName)}");
        }

        public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            OpenReadCalls++;
            return Task.FromResult<Stream?>(new MemoryStream([1, 2, 3]));
        }
    }
}
