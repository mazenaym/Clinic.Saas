/*
Tenant-aware foreign key migration plan

Purpose:
- Keep Id as the primary key for the current schema.
- Add unique candidate keys on (TenantId, Id) for tenant-owned tables.
- Replace single-column tenant-owned foreign keys with composite foreign keys.

Review before running:
- This file is a plan. Do not run it against production until the preflight checks are reviewed.
- The preflight blocks intentionally fail fast when existing rows reference a record from another tenant.
- Child tables without TenantId, such as PaymentItems and PrescriptionItems, are not changed in this plan.
*/

SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    -------------------------------------------------------------------------
    -- Preflight: old single-column FKs allow an Appointment to point to a
    -- Patient or Doctor from another tenant. Stop before changing constraints
    -- if any existing cross-tenant references already exist.
    -------------------------------------------------------------------------
    IF EXISTS
    (
        SELECT 1
        FROM dbo.Appointments a
        INNER JOIN dbo.Patients p ON p.Id = a.PatientId
        WHERE a.TenantId <> p.TenantId
    )
        THROW 51000, 'Cross-tenant Appointment -> Patient references exist. Fix data before applying tenant-aware FKs.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Appointments a
        INNER JOIN dbo.Users u ON u.Id = a.DoctorId
        WHERE a.TenantId <> u.TenantId
    )
        THROW 51001, 'Cross-tenant Appointment -> User references exist. Fix data before applying tenant-aware FKs.', 1;

    -------------------------------------------------------------------------
    -- Preflight: old single-column FKs allow a Visit to point to a Patient,
    -- Doctor, or Appointment from another tenant. AppointmentId is nullable,
    -- so only populated links are checked.
    -------------------------------------------------------------------------
    IF EXISTS
    (
        SELECT 1
        FROM dbo.Visits v
        INNER JOIN dbo.Patients p ON p.Id = v.PatientId
        WHERE v.TenantId <> p.TenantId
    )
        THROW 51002, 'Cross-tenant Visit -> Patient references exist. Fix data before applying tenant-aware FKs.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Visits v
        INNER JOIN dbo.Users u ON u.Id = v.DoctorId
        WHERE v.TenantId <> u.TenantId
    )
        THROW 51003, 'Cross-tenant Visit -> User references exist. Fix data before applying tenant-aware FKs.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Visits v
        INNER JOIN dbo.Appointments a ON a.Id = v.AppointmentId
        WHERE v.AppointmentId IS NOT NULL
          AND v.TenantId <> a.TenantId
    )
        THROW 51004, 'Cross-tenant Visit -> Appointment references exist. Fix data before applying tenant-aware FKs.', 1;

    -------------------------------------------------------------------------
    -- Preflight: old single-column FKs allow a Prescription to point to a
    -- Visit, Patient, or Doctor from another tenant.
    -------------------------------------------------------------------------
    IF EXISTS
    (
        SELECT 1
        FROM dbo.Prescriptions pr
        INNER JOIN dbo.Visits v ON v.Id = pr.VisitId
        WHERE pr.TenantId <> v.TenantId
    )
        THROW 51005, 'Cross-tenant Prescription -> Visit references exist. Fix data before applying tenant-aware FKs.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Prescriptions pr
        INNER JOIN dbo.Patients p ON p.Id = pr.PatientId
        WHERE pr.TenantId <> p.TenantId
    )
        THROW 51006, 'Cross-tenant Prescription -> Patient references exist. Fix data before applying tenant-aware FKs.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Prescriptions pr
        INNER JOIN dbo.Users u ON u.Id = pr.DoctorId
        WHERE pr.TenantId <> u.TenantId
    )
        THROW 51007, 'Cross-tenant Prescription -> User references exist. Fix data before applying tenant-aware FKs.', 1;

    -------------------------------------------------------------------------
    -- Preflight: old single-column FKs allow a Payment to point to a Visit or
    -- Patient from another tenant.
    -------------------------------------------------------------------------
    IF EXISTS
    (
        SELECT 1
        FROM dbo.Payments pay
        INNER JOIN dbo.Visits v ON v.Id = pay.VisitId
        WHERE pay.TenantId <> v.TenantId
    )
        THROW 51008, 'Cross-tenant Payment -> Visit references exist. Fix data before applying tenant-aware FKs.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Payments pay
        INNER JOIN dbo.Patients p ON p.Id = pay.PatientId
        WHERE pay.TenantId <> p.TenantId
    )
        THROW 51009, 'Cross-tenant Payment -> Patient references exist. Fix data before applying tenant-aware FKs.', 1;

    -------------------------------------------------------------------------
    -- Preflight: PatientDocuments was added after the base schema. If it
    -- exists, stop if any document points at a patient from another tenant.
    -------------------------------------------------------------------------
    IF OBJECT_ID(N'dbo.PatientDocuments', N'U') IS NOT NULL
       AND EXISTS
       (
           SELECT 1
           FROM dbo.PatientDocuments d
           INNER JOIN dbo.Patients p ON p.Id = d.PatientId
           WHERE d.TenantId <> p.TenantId
       )
        THROW 51010, 'Cross-tenant PatientDocument -> Patient references exist. Fix data before applying tenant-aware FKs.', 1;

    -------------------------------------------------------------------------
    -- Candidate keys: composite FKs need a unique referenced key on
    -- (TenantId, Id). These indexes are redundant with Id primary keys for
    -- uniqueness, but they give SQL Server a tenant-aware parent key.
    -------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Subscriptions') AND name = N'UX_Subscriptions_Tenant_Id')
        CREATE UNIQUE INDEX UX_Subscriptions_Tenant_Id ON dbo.Subscriptions(TenantId, Id);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = N'UX_Users_Tenant_Id')
        CREATE UNIQUE INDEX UX_Users_Tenant_Id ON dbo.Users(TenantId, Id);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Patients') AND name = N'UX_Patients_Tenant_Id')
        CREATE UNIQUE INDEX UX_Patients_Tenant_Id ON dbo.Patients(TenantId, Id);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Appointments') AND name = N'UX_Appointments_Tenant_Id')
        CREATE UNIQUE INDEX UX_Appointments_Tenant_Id ON dbo.Appointments(TenantId, Id);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Visits') AND name = N'UX_Visits_Tenant_Id')
        CREATE UNIQUE INDEX UX_Visits_Tenant_Id ON dbo.Visits(TenantId, Id);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Prescriptions') AND name = N'UX_Prescriptions_Tenant_Id')
        CREATE UNIQUE INDEX UX_Prescriptions_Tenant_Id ON dbo.Prescriptions(TenantId, Id);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Payments') AND name = N'UX_Payments_Tenant_Id')
        CREATE UNIQUE INDEX UX_Payments_Tenant_Id ON dbo.Payments(TenantId, Id);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ClinicSettings') AND name = N'UX_ClinicSettings_Tenant_Id')
        CREATE UNIQUE INDEX UX_ClinicSettings_Tenant_Id ON dbo.ClinicSettings(TenantId, Id);

    IF OBJECT_ID(N'dbo.PatientDocuments', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PatientDocuments') AND name = N'UX_PatientDocuments_Tenant_Id')
        CREATE UNIQUE INDEX UX_PatientDocuments_Tenant_Id ON dbo.PatientDocuments(TenantId, Id);

    IF OBJECT_ID(N'dbo.OnlineBookings', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.OnlineBookings') AND name = N'UX_OnlineBookings_Tenant_Id')
        CREATE UNIQUE INDEX UX_OnlineBookings_Tenant_Id ON dbo.OnlineBookings(TenantId, Id);

    IF OBJECT_ID(N'dbo.ClinicalTemplates', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ClinicalTemplates') AND name = N'UX_ClinicalTemplates_Tenant_Id')
        CREATE UNIQUE INDEX UX_ClinicalTemplates_Tenant_Id ON dbo.ClinicalTemplates(TenantId, Id);

    -------------------------------------------------------------------------
    -- Appointments: replace single-column PatientId/DoctorId FKs so an
    -- appointment cannot reference a patient or doctor belonging to another
    -- tenant.
    -------------------------------------------------------------------------
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Appointments_Patients' AND parent_object_id = OBJECT_ID(N'dbo.Appointments'))
        ALTER TABLE dbo.Appointments DROP CONSTRAINT FK_Appointments_Patients;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Appointments_Patients_Tenant' AND parent_object_id = OBJECT_ID(N'dbo.Appointments'))
        ALTER TABLE dbo.Appointments WITH CHECK ADD CONSTRAINT FK_Appointments_Patients_Tenant
            FOREIGN KEY (TenantId, PatientId) REFERENCES dbo.Patients(TenantId, Id);

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Appointments_Doctors' AND parent_object_id = OBJECT_ID(N'dbo.Appointments'))
        ALTER TABLE dbo.Appointments DROP CONSTRAINT FK_Appointments_Doctors;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Appointments_Doctors_Tenant' AND parent_object_id = OBJECT_ID(N'dbo.Appointments'))
        ALTER TABLE dbo.Appointments WITH CHECK ADD CONSTRAINT FK_Appointments_Doctors_Tenant
            FOREIGN KEY (TenantId, DoctorId) REFERENCES dbo.Users(TenantId, Id);

    -------------------------------------------------------------------------
    -- Visits: replace single-column PatientId/DoctorId/AppointmentId FKs so a
    -- visit cannot be linked to clinical or scheduling records across tenants.
    -------------------------------------------------------------------------
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Visits_Patients' AND parent_object_id = OBJECT_ID(N'dbo.Visits'))
        ALTER TABLE dbo.Visits DROP CONSTRAINT FK_Visits_Patients;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Visits_Patients_Tenant' AND parent_object_id = OBJECT_ID(N'dbo.Visits'))
        ALTER TABLE dbo.Visits WITH CHECK ADD CONSTRAINT FK_Visits_Patients_Tenant
            FOREIGN KEY (TenantId, PatientId) REFERENCES dbo.Patients(TenantId, Id);

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Visits_Doctors' AND parent_object_id = OBJECT_ID(N'dbo.Visits'))
        ALTER TABLE dbo.Visits DROP CONSTRAINT FK_Visits_Doctors;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Visits_Doctors_Tenant' AND parent_object_id = OBJECT_ID(N'dbo.Visits'))
        ALTER TABLE dbo.Visits WITH CHECK ADD CONSTRAINT FK_Visits_Doctors_Tenant
            FOREIGN KEY (TenantId, DoctorId) REFERENCES dbo.Users(TenantId, Id);

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Visits_Appointments' AND parent_object_id = OBJECT_ID(N'dbo.Visits'))
        ALTER TABLE dbo.Visits DROP CONSTRAINT FK_Visits_Appointments;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Visits_Appointments_Tenant' AND parent_object_id = OBJECT_ID(N'dbo.Visits'))
        ALTER TABLE dbo.Visits WITH CHECK ADD CONSTRAINT FK_Visits_Appointments_Tenant
            FOREIGN KEY (TenantId, AppointmentId) REFERENCES dbo.Appointments(TenantId, Id);

    -------------------------------------------------------------------------
    -- Prescriptions: replace single-column VisitId/PatientId/DoctorId FKs so
    -- medication records cannot attach to clinical records from another tenant.
    -------------------------------------------------------------------------
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Prescriptions_Visits' AND parent_object_id = OBJECT_ID(N'dbo.Prescriptions'))
        ALTER TABLE dbo.Prescriptions DROP CONSTRAINT FK_Prescriptions_Visits;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Prescriptions_Visits_Tenant' AND parent_object_id = OBJECT_ID(N'dbo.Prescriptions'))
        ALTER TABLE dbo.Prescriptions WITH CHECK ADD CONSTRAINT FK_Prescriptions_Visits_Tenant
            FOREIGN KEY (TenantId, VisitId) REFERENCES dbo.Visits(TenantId, Id);

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Prescriptions_Patients' AND parent_object_id = OBJECT_ID(N'dbo.Prescriptions'))
        ALTER TABLE dbo.Prescriptions DROP CONSTRAINT FK_Prescriptions_Patients;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Prescriptions_Patients_Tenant' AND parent_object_id = OBJECT_ID(N'dbo.Prescriptions'))
        ALTER TABLE dbo.Prescriptions WITH CHECK ADD CONSTRAINT FK_Prescriptions_Patients_Tenant
            FOREIGN KEY (TenantId, PatientId) REFERENCES dbo.Patients(TenantId, Id);

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Prescriptions_Doctors' AND parent_object_id = OBJECT_ID(N'dbo.Prescriptions'))
        ALTER TABLE dbo.Prescriptions DROP CONSTRAINT FK_Prescriptions_Doctors;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Prescriptions_Doctors_Tenant' AND parent_object_id = OBJECT_ID(N'dbo.Prescriptions'))
        ALTER TABLE dbo.Prescriptions WITH CHECK ADD CONSTRAINT FK_Prescriptions_Doctors_Tenant
            FOREIGN KEY (TenantId, DoctorId) REFERENCES dbo.Users(TenantId, Id);

    -------------------------------------------------------------------------
    -- Payments: replace single-column VisitId/PatientId FKs so invoices and
    -- receipts cannot point to clinical or patient records from another tenant.
    -------------------------------------------------------------------------
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Payments_Visits' AND parent_object_id = OBJECT_ID(N'dbo.Payments'))
        ALTER TABLE dbo.Payments DROP CONSTRAINT FK_Payments_Visits;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Payments_Visits_Tenant' AND parent_object_id = OBJECT_ID(N'dbo.Payments'))
        ALTER TABLE dbo.Payments WITH CHECK ADD CONSTRAINT FK_Payments_Visits_Tenant
            FOREIGN KEY (TenantId, VisitId) REFERENCES dbo.Visits(TenantId, Id);

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Payments_Patients' AND parent_object_id = OBJECT_ID(N'dbo.Payments'))
        ALTER TABLE dbo.Payments DROP CONSTRAINT FK_Payments_Patients;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Payments_Patients_Tenant' AND parent_object_id = OBJECT_ID(N'dbo.Payments'))
        ALTER TABLE dbo.Payments WITH CHECK ADD CONSTRAINT FK_Payments_Patients_Tenant
            FOREIGN KEY (TenantId, PatientId) REFERENCES dbo.Patients(TenantId, Id);

    -------------------------------------------------------------------------
    -- PatientDocuments: replace the single-column PatientId FK so uploaded
    -- medical files cannot attach to a patient from another tenant.
    -------------------------------------------------------------------------
    IF OBJECT_ID(N'dbo.PatientDocuments', N'U') IS NOT NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PatientDocuments_Patients' AND parent_object_id = OBJECT_ID(N'dbo.PatientDocuments'))
            ALTER TABLE dbo.PatientDocuments DROP CONSTRAINT FK_PatientDocuments_Patients;

        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PatientDocuments_Patients_Tenant' AND parent_object_id = OBJECT_ID(N'dbo.PatientDocuments'))
            ALTER TABLE dbo.PatientDocuments WITH CHECK ADD CONSTRAINT FK_PatientDocuments_Patients_Tenant
                FOREIGN KEY (TenantId, PatientId) REFERENCES dbo.Patients(TenantId, Id);
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO
