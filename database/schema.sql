CREATE TABLE dbo.Tenants
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Tenants PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Subdomain NVARCHAR(100) NOT NULL,
    Email NVARCHAR(256) NOT NULL,
    Phone NVARCHAR(50) NULL,
    LogoUrl NVARCHAR(1000) NULL,
    [Plan] SMALLINT NOT NULL,
    TimeZone NVARCHAR(100) NOT NULL CONSTRAINT DF_Tenants_TimeZone DEFAULT N'Africa/Cairo',
    Currency NVARCHAR(10) NOT NULL CONSTRAINT DF_Tenants_Currency DEFAULT N'EGP',
    IsActive BIT NOT NULL CONSTRAINT DF_Tenants_IsActive DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL
);

CREATE UNIQUE INDEX UX_Tenants_Subdomain ON dbo.Tenants(Subdomain);

CREATE TABLE dbo.Users
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    FullName NVARCHAR(200) NOT NULL,
    Email NVARCHAR(256) NOT NULL,
    PasswordHash NVARCHAR(500) NOT NULL,
    [Role] SMALLINT NOT NULL,
    Phone NVARCHAR(50) NULL,
    Specialty NVARCHAR(100) NULL,
    LicenseNumber NVARCHAR(100) NULL,
    AvatarUrl NVARCHAR(1000) NULL,
    RefreshToken NVARCHAR(500) NULL,
    RefreshTokenExpiry DATETIME2 NULL,
    FailedLoginAttempts INT NOT NULL CONSTRAINT DF_Users_FailedLoginAttempts DEFAULT 0,
    LockedUntil DATETIME2 NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    CONSTRAINT FK_Users_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id)
);

CREATE UNIQUE INDEX UX_Users_Tenant_Email ON dbo.Users(TenantId, Email);

CREATE TABLE dbo.Patients
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Patients PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    PatientCode NVARCHAR(20) NOT NULL,
    FullName NVARCHAR(200) NOT NULL,
    PhoneNumber NVARCHAR(50) NOT NULL,
    DateOfBirth DATE NULL,
    Gender SMALLINT NOT NULL,
    BloodType NVARCHAR(10) NULL,
    NationalId NVARCHAR(50) NULL,
    Email NVARCHAR(256) NULL,
    Address NVARCHAR(500) NULL,
    MedicalHistory NVARCHAR(MAX) NULL,
    DrugAllergies NVARCHAR(MAX) NULL,
    ChronicDiseases NVARCHAR(MAX) NULL,
    EmergencyContactName NVARCHAR(200) NULL,
    EmergencyContactPhone NVARCHAR(50) NULL,
    InsuranceCompany NVARCHAR(200) NULL,
    InsuranceNumber NVARCHAR(100) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Patients_IsActive DEFAULT 1,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Patients_IsDeleted DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    CreatedBy UNIQUEIDENTIFIER NULL,
    CONSTRAINT FK_Patients_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id)
);

CREATE UNIQUE INDEX UX_Patients_Tenant_Code ON dbo.Patients(TenantId, PatientCode);
CREATE INDEX IX_Patients_Tenant_Search ON dbo.Patients(TenantId, IsDeleted, FullName, PhoneNumber);

CREATE TABLE dbo.Appointments
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Appointments PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    PatientId UNIQUEIDENTIFIER NOT NULL,
    DoctorId UNIQUEIDENTIFIER NOT NULL,
    AppointmentDate DATE NOT NULL,
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    [Status] SMALLINT NOT NULL,
    [Type] SMALLINT NOT NULL,
    [Source] SMALLINT NOT NULL,
    Notes NVARCHAR(MAX) NULL,
    CancelReason NVARCHAR(500) NULL,
    ReminderSent BIT NOT NULL CONSTRAINT DF_Appointments_ReminderSent DEFAULT 0,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Appointments_IsDeleted DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    CreatedBy UNIQUEIDENTIFIER NULL,
    CONSTRAINT FK_Appointments_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
    CONSTRAINT FK_Appointments_Patients FOREIGN KEY (PatientId) REFERENCES dbo.Patients(Id),
    CONSTRAINT FK_Appointments_Doctors FOREIGN KEY (DoctorId) REFERENCES dbo.Users(Id),
    CONSTRAINT CK_Appointments_TimeRange CHECK (StartTime < EndTime)
);

CREATE INDEX IX_Appointments_Tenant_Date ON dbo.Appointments(TenantId, AppointmentDate, IsDeleted);
CREATE INDEX IX_Appointments_Tenant_Doctor_Date ON dbo.Appointments(TenantId, DoctorId, AppointmentDate, IsDeleted, StartTime, EndTime);

CREATE TABLE dbo.Visits
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Visits PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    PatientId UNIQUEIDENTIFIER NOT NULL,
    AppointmentId UNIQUEIDENTIFIER NULL,
    DoctorId UNIQUEIDENTIFIER NOT NULL,
    VisitDate DATETIME2 NOT NULL,
    VisitType SMALLINT NOT NULL,
    ChiefComplaint NVARCHAR(1000) NOT NULL,
    VitalSigns NVARCHAR(MAX) NULL,
    ClinicalNotes NVARCHAR(MAX) NULL,
    Diagnosis NVARCHAR(MAX) NULL,
    DiagnosisCode NVARCHAR(50) NULL,
    DifferentialDx NVARCHAR(MAX) NULL,
    FollowUpDate DATETIME2 NULL,
    FollowUpNotes NVARCHAR(MAX) NULL,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Visits_IsDeleted DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    CONSTRAINT FK_Visits_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
    CONSTRAINT FK_Visits_Patients FOREIGN KEY (PatientId) REFERENCES dbo.Patients(Id),
    CONSTRAINT FK_Visits_Doctors FOREIGN KEY (DoctorId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_Visits_Appointments FOREIGN KEY (AppointmentId) REFERENCES dbo.Appointments(Id)
);

CREATE INDEX IX_Visits_Tenant_Date ON dbo.Visits(TenantId, VisitDate, IsDeleted);
CREATE INDEX IX_Visits_Tenant_Patient ON dbo.Visits(TenantId, PatientId, IsDeleted, VisitDate);

CREATE TABLE dbo.Prescriptions
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Prescriptions PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    VisitId UNIQUEIDENTIFIER NOT NULL,
    PatientId UNIQUEIDENTIFIER NOT NULL,
    DoctorId UNIQUEIDENTIFIER NOT NULL,
    Notes NVARCHAR(MAX) NULL,
    QrCode NVARCHAR(1000) NULL,
    PdfUrl NVARCHAR(1000) NULL,
    SentViaWhatsapp BIT NOT NULL CONSTRAINT DF_Prescriptions_SentViaWhatsapp DEFAULT 0,
    SentViaSms BIT NOT NULL CONSTRAINT DF_Prescriptions_SentViaSms DEFAULT 0,
    IsActive BIT NOT NULL CONSTRAINT DF_Prescriptions_IsActive DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL,
    CONSTRAINT FK_Prescriptions_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
    CONSTRAINT FK_Prescriptions_Visits FOREIGN KEY (VisitId) REFERENCES dbo.Visits(Id),
    CONSTRAINT FK_Prescriptions_Patients FOREIGN KEY (PatientId) REFERENCES dbo.Patients(Id),
    CONSTRAINT FK_Prescriptions_Doctors FOREIGN KEY (DoctorId) REFERENCES dbo.Users(Id)
);

CREATE TABLE dbo.PrescriptionItems
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PrescriptionItems PRIMARY KEY,
    PrescriptionId UNIQUEIDENTIFIER NOT NULL,
    DrugId UNIQUEIDENTIFIER NULL,
    DrugName NVARCHAR(200) NOT NULL,
    Dosage NVARCHAR(100) NOT NULL,
    Frequency NVARCHAR(100) NOT NULL,
    Duration NVARCHAR(100) NOT NULL,
    [Route] NVARCHAR(100) NULL,
    Instructions NVARCHAR(MAX) NULL,
    SortOrder INT NOT NULL,
    CONSTRAINT FK_PrescriptionItems_Prescriptions FOREIGN KEY (PrescriptionId) REFERENCES dbo.Prescriptions(Id)
);

CREATE TABLE dbo.Payments
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Payments PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    VisitId UNIQUEIDENTIFIER NOT NULL,
    PatientId UNIQUEIDENTIFIER NOT NULL,
    InvoiceNumber NVARCHAR(30) NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    DiscountAmount DECIMAL(18,2) NOT NULL,
    DiscountPct DECIMAL(5,2) NOT NULL,
    TaxAmount DECIMAL(18,2) NOT NULL,
    PaidAmount DECIMAL(18,2) NOT NULL,
    RemainingAmount DECIMAL(18,2) NOT NULL,
    PaymentMethod SMALLINT NOT NULL,
    [Status] SMALLINT NOT NULL,
    InsuranceCompany NVARCHAR(200) NULL,
    InsuranceNumber NVARCHAR(100) NULL,
    ReceiptUrl NVARCHAR(1000) NULL,
    Notes NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    CreatedBy UNIQUEIDENTIFIER NULL,
    CONSTRAINT FK_Payments_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
    CONSTRAINT FK_Payments_Visits FOREIGN KEY (VisitId) REFERENCES dbo.Visits(Id),
    CONSTRAINT FK_Payments_Patients FOREIGN KEY (PatientId) REFERENCES dbo.Patients(Id)
);

CREATE UNIQUE INDEX UX_Payments_Tenant_Invoice ON dbo.Payments(TenantId, InvoiceNumber);
CREATE INDEX IX_Payments_Tenant_CreatedAt ON dbo.Payments(TenantId, CreatedAt);

CREATE TABLE dbo.PaymentItems
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PaymentItems PRIMARY KEY,
    PaymentId UNIQUEIDENTIFIER NOT NULL,
    ServiceName NVARCHAR(200) NOT NULL,
    ServiceType SMALLINT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    DiscountPct DECIMAL(5,2) NOT NULL,
    TotalPrice DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_PaymentItems_Payments FOREIGN KEY (PaymentId) REFERENCES dbo.Payments(Id)
);

CREATE TABLE dbo.ClinicSettings
(
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ClinicSettings PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    WorkingDays NVARCHAR(7) NOT NULL CONSTRAINT DF_ClinicSettings_WorkingDays DEFAULT N'0111110',
    OpenTime TIME NOT NULL,
    CloseTime TIME NOT NULL,
    SlotDurationMin INT NOT NULL CONSTRAINT DF_ClinicSettings_SlotDuration DEFAULT 20,
    ConsultFee DECIMAL(18,2) NOT NULL,
    SmsEnabled BIT NOT NULL CONSTRAINT DF_ClinicSettings_SmsEnabled DEFAULT 0,
    WhatsappEnabled BIT NOT NULL CONSTRAINT DF_ClinicSettings_WhatsappEnabled DEFAULT 0,
    EmailEnabled BIT NOT NULL CONSTRAINT DF_ClinicSettings_EmailEnabled DEFAULT 0,
    [Language] NVARCHAR(10) NOT NULL CONSTRAINT DF_ClinicSettings_Language DEFAULT N'ar',
    TaxPct DECIMAL(5,2) NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    CONSTRAINT FK_ClinicSettings_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id)
);

CREATE UNIQUE INDEX UX_ClinicSettings_Tenant ON dbo.ClinicSettings(TenantId);
