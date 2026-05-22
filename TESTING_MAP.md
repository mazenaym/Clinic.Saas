# Clinic SaaS API Testing Map

Base URLs:

- HTTP: `http://localhost:5232`
- HTTPS: `https://localhost:7299`

Common headers after login:

```http
Authorization: Bearer {{accessToken}}
Content-Type: application/json
```

Enum values used in requests:

- `UserRole`: Admin = 1, Doctor = 2, Reception = 3
- `Gender`: Male = 1, Female = 2
- `AppointmentStatus`: Scheduled = 1, Confirmed = 2, Completed = 3, Cancelled = 4, NoShow = 5
- `AppointmentType`: New = 1, FollowUp = 2, Emergency = 3, Telemedicine = 4
- `AppointmentSource`: Reception = 1, OnlinePortal = 2, Phone = 3, WalkIn = 4
- `VisitType`: New = 1, FollowUp = 2, Emergency = 3, Routine = 4
- `PaymentMethod`: Cash = 1, Card = 2, BankTransfer = 3, Insurance = 4, Mixed = 5
- `ServiceType`: Consultation = 1, Lab = 2, Radiology = 3, Procedure = 4, Drug = 5, Other = 6

## 0. Seed Data

There is no public tenant onboarding endpoint yet, so start with two tenants and one admin in each tenant.

Test password for all seeded users: `P@ssw0rd!`

```sql
DECLARE @TenantA UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @TenantB UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';
DECLARE @AdminA UNIQUEIDENTIFIER = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
DECLARE @AdminB UNIQUEIDENTIFIER = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
DECLARE @PasswordHash NVARCHAR(500) = 'AQAAAAEAACcQAAAAEJ2rrkH0tyyQUfHZC1gPDyHwZP/rKTCCyTbCULVfykokvzhPFu+0w8RtdkDsy0w1yQ==';

IF NOT EXISTS (SELECT 1 FROM dbo.Tenants WHERE Id = @TenantA)
INSERT INTO dbo.Tenants
(Id, Name, Subdomain, Email, Phone, [Plan], TimeZone, Currency, IsActive, CreatedAt, UpdatedAt)
VALUES
(@TenantA, N'Cairo Smile Clinic', N'cairo-smile', N'admin@cairosmile.test', N'01000000001', 2, N'Africa/Cairo', N'EGP', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Tenants WHERE Id = @TenantB)
INSERT INTO dbo.Tenants
(Id, Name, Subdomain, Email, Phone, [Plan], TimeZone, Currency, IsActive, CreatedAt, UpdatedAt)
VALUES
(@TenantB, N'Alex Care Clinic', N'alex-care', N'admin@alexcare.test', N'01000000002', 1, N'Africa/Cairo', N'EGP', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = @AdminA)
INSERT INTO dbo.Users
(Id, TenantId, FullName, Email, PasswordHash, [Role], Phone, IsActive, FailedLoginAttempts, CreatedAt, UpdatedAt)
VALUES
(@AdminA, @TenantA, N'Cairo Admin', N'admin@cairosmile.test', @PasswordHash, 1, N'01000000001', 1, 0, SYSUTCDATETIME(), SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = @AdminB)
INSERT INTO dbo.Users
(Id, TenantId, FullName, Email, PasswordHash, [Role], Phone, IsActive, FailedLoginAttempts, CreatedAt, UpdatedAt)
VALUES
(@AdminB, @TenantB, N'Alex Admin', N'admin@alexcare.test', @PasswordHash, 1, N'01000000002', 1, 0, SYSUTCDATETIME(), SYSUTCDATETIME());
```

## 1. Auth Tests

### 1.1 Login success, Tenant A admin

`POST /api/auth/login`

```json
{
  "email": "admin@cairosmile.test",
  "password": "P@ssw0rd!",
  "subdomain": "cairo-smile"
}
```

Expected:

- Status: `200`
- `success = true`
- Save `data.accessToken` as `adminTokenA`
- Save `data.refreshToken` as `refreshTokenA`
- Save `data.user.id` as `adminAId`
- Save `data.tenant.id` as `tenantAId`

### 1.2 Login success, Tenant B admin

Same endpoint:

```json
{
  "email": "admin@alexcare.test",
  "password": "P@ssw0rd!",
  "subdomain": "alex-care"
}
```

Expected `200`. Save token as `adminTokenB`.

### 1.3 Wrong password

```json
{
  "email": "admin@cairosmile.test",
  "password": "WrongPass123",
  "subdomain": "cairo-smile"
}
```

Expected:

- Status: `401`
- `success = false`

### 1.4 Wrong tenant isolation at login

```json
{
  "email": "admin@cairosmile.test",
  "password": "P@ssw0rd!",
  "subdomain": "alex-care"
}
```

Expected:

- Status: `401`
- This proves user email is scoped by tenant.

### 1.5 Missing subdomain

```json
{
  "email": "admin@cairosmile.test",
  "password": "P@ssw0rd!"
}
```

Expected on localhost:

- Status: `400`
- Validation error for subdomain.

### 1.6 Refresh token

`POST /api/auth/refresh`

```json
{
  "refreshToken": "{{refreshTokenA}}"
}
```

Expected:

- Status: `200`
- New `accessToken` and new `refreshToken`.
- Old refresh token should fail if retried.

### 1.7 Logout

`POST /api/auth/logout`

Headers: `Authorization: Bearer {{adminTokenA}}`

Expected:

- Status: `200`
- The current refresh token for this user should no longer refresh.

## 2. Users Tests

Use `adminTokenA` unless stated.

### 2.1 Create doctor

`POST /api/users`

```json
{
  "fullName": "Dr. Omar Hassan",
  "email": "omar.doctor@cairosmile.test",
  "password": "P@ssw0rd!",
  "role": 2,
  "phone": "01012345678",
  "specialty": "Dentistry",
  "licenseNumber": "DOC-CAI-001"
}
```

Expected:

- Status: `201`
- Save `data.id` as `doctorAId`.

### 2.2 Create reception user

```json
{
  "fullName": "Mona Reception",
  "email": "mona.reception@cairosmile.test",
  "password": "P@ssw0rd!",
  "role": 3,
  "phone": "01012345679"
}
```

Expected:

- Status: `201`
- Save `data.id` as `receptionAId`.

### 2.3 Duplicate email in same tenant

Repeat doctor creation with the same email.

Expected:

- Status: `409`

### 2.4 Invalid user payload

```json
{
  "fullName": "Om",
  "email": "not-email",
  "password": "123",
  "role": 99
}
```

Expected:

- Status: `400`

### 2.5 List tenant users

`GET /api/users`

Expected:

- Status: `200`
- Contains only Tenant A users.

### 2.6 Current user

`GET /api/users/me`

Expected:

- Status: `200`
- `data.email = admin@cairosmile.test`

### 2.7 Role denied

Login as doctor:

```json
{
  "email": "omar.doctor@cairosmile.test",
  "password": "P@ssw0rd!",
  "subdomain": "cairo-smile"
}
```

Call `GET /api/users` using doctor token.

Expected:

- Status: `403`

## 3. Patients Tests

### 3.1 Create patient success

`POST /api/patients`

```json
{
  "fullName": "Ahmed Mahmoud",
  "phoneNumber": "01012345670",
  "dateOfBirth": "1990-05-15",
  "gender": 1,
  "bloodType": "O+",
  "nationalId": "29005150123456",
  "email": "ahmed.patient@test.local",
  "address": "Nasr City, Cairo",
  "medicalHistory": "No previous surgeries",
  "drugAllergies": "Penicillin",
  "chronicDiseases": "None",
  "emergencyContactName": "Sara Mahmoud",
  "emergencyContactPhone": "01012345671",
  "insuranceCompany": "Test Insurance",
  "insuranceNumber": "INS-001"
}
```

Expected:

- Status: `201`
- `data.patientCode` starts with `CLN-`
- Save `data.id` as `patientAId`.

### 3.2 Duplicate phone in same tenant

Repeat same patient creation.

Expected:

- Status: `409`

### 3.3 Invalid phone

```json
{
  "fullName": "Bad Phone",
  "phoneNumber": "12345",
  "gender": 1
}
```

Expected:

- Status: `400`

### 3.4 Invalid email

```json
{
  "fullName": "Invalid Email Patient",
  "phoneNumber": "01012345672",
  "gender": 2,
  "email": "wrong-email"
}
```

Expected:

- Status: `400`

### 3.5 Get patient by id

`GET /api/patients/{{patientAId}}`

Expected:

- Status: `200`
- `data.id = patientAId`

### 3.6 List patients

`GET /api/patients`

Expected:

- Status: `200`
- Contains `Ahmed Mahmoud`.

### 3.7 Search patients by name

`GET /api/patients/search?term=Ahmed`

Expected:

- Status: `200`
- Result contains `Ahmed Mahmoud`.

### 3.8 Search patients by phone

`GET /api/patients/search?term=01012345670`

Expected:

- Status: `200`
- Result contains one matching patient.

### 3.9 Update patient

`PUT /api/patients/{{patientAId}}`

```json
{
  "fullName": "Ahmed Mahmoud Updated",
  "phoneNumber": "01012345670",
  "dateOfBirth": "1990-05-15",
  "gender": 1,
  "bloodType": "A+",
  "email": "ahmed.updated@test.local",
  "address": "Heliopolis, Cairo",
  "medicalHistory": "Appendectomy 2010",
  "drugAllergies": "Penicillin",
  "chronicDiseases": "None",
  "emergencyContactName": "Sara Mahmoud",
  "emergencyContactPhone": "01012345671",
  "insuranceCompany": "Test Insurance",
  "insuranceNumber": "INS-001"
}
```

Expected:

- Status: `200`
- Returned patient name and blood type updated.

### 3.10 Delete patient denied for Doctor

Use doctor token:

`DELETE /api/patients/{{patientAId}}`

Expected:

- Status: `403`

Do not delete the main patient before appointments/visits tests.

## 4. Appointments Tests

### 4.1 Availability before booking

`GET /api/appointments/availability?doctorId={{doctorAId}}&date=2026-06-01`

Expected:

- Status: `200`
- Contains many slots from 09:00 to 17:00.

### 4.2 Create appointment success

`POST /api/appointments`

```json
{
  "patientId": "{{patientAId}}",
  "doctorId": "{{doctorAId}}",
  "appointmentDate": "2026-06-01",
  "startTime": "10:00:00",
  "endTime": "10:30:00",
  "type": 1,
  "source": 1,
  "notes": "First consultation"
}
```

Expected:

- Status: `201`
- Save `data.id` as `appointmentAId`.
- `data.patientName` and `data.doctorName` are not empty.

### 4.3 Conflict appointment same doctor/time

Repeat the same appointment payload.

Expected:

- Status: `409`

### 4.4 Appointment with end time before start time

```json
{
  "patientId": "{{patientAId}}",
  "doctorId": "{{doctorAId}}",
  "appointmentDate": "2026-06-01",
  "startTime": "11:00:00",
  "endTime": "10:30:00",
  "type": 1,
  "source": 1
}
```

Expected:

- Status: `400`

### 4.5 Appointment in the past

```json
{
  "patientId": "{{patientAId}}",
  "doctorId": "{{doctorAId}}",
  "appointmentDate": "2020-01-01",
  "startTime": "10:00:00",
  "endTime": "10:30:00",
  "type": 1,
  "source": 1
}
```

Expected:

- Status: `400`

### 4.6 Daily appointments

`GET /api/appointments/daily?date=2026-06-01`

Expected:

- Status: `200`
- Contains `appointmentAId`.

### 4.7 Availability after booking

`GET /api/appointments/availability?doctorId={{doctorAId}}&date=2026-06-01`

Expected:

- Status: `200`
- Slot `10:00 - 10:30` is not available.

### 4.8 Confirm appointment

`PUT /api/appointments/{{appointmentAId}}/status`

```json
{
  "status": 2,
  "cancelReason": null
}
```

Expected:

- Status: `200`

### 4.9 Cancel appointment

Create another appointment at 11:00, save as `appointmentCancelId`, then:

`PUT /api/appointments/{{appointmentCancelId}}/status`

```json
{
  "status": 4,
  "cancelReason": "Patient requested cancellation"
}
```

Expected:

- Status: `200`

### 4.10 Update status for random id

`PUT /api/appointments/99999999-9999-9999-9999-999999999999/status`

```json
{
  "status": 2
}
```

Expected:

- Status: `404`

## 5. Visits Tests

### 5.1 Create visit linked to appointment

Use admin or doctor token.

`POST /api/visits`

```json
{
  "patientId": "{{patientAId}}",
  "appointmentId": "{{appointmentAId}}",
  "doctorId": "{{doctorAId}}",
  "visitType": 1,
  "chiefComplaint": "Tooth pain for 3 days",
  "vitalSigns": {
    "bloodPressure": "120/80",
    "temperature": 37.1,
    "weight": 82.5,
    "height": 178,
    "pulse": 78,
    "spO2": 98,
    "rbs": 110
  },
  "clinicalNotes": "Pain on chewing, no swelling",
  "diagnosis": "Dental caries",
  "diagnosisCode": "K02",
  "followUpDate": "2026-06-08"
}
```

Expected:

- Status: `201`
- Save `data.id` as `visitAId`.
- Appointment status should become `Completed` in daily appointments.

### 5.2 Missing chief complaint

```json
{
  "patientId": "{{patientAId}}",
  "doctorId": "{{doctorAId}}",
  "visitType": 1,
  "chiefComplaint": ""
}
```

Expected:

- Status: `400`

### 5.3 Get visit by id

`GET /api/visits/{{visitAId}}`

Expected:

- Status: `200`
- `data.patientName` and `data.doctorName` are not empty.

### 5.4 Get random visit

`GET /api/visits/99999999-9999-9999-9999-999999999999`

Expected:

- Status: `404`

## 6. Prescriptions Tests

### 6.1 Create prescription success

`POST /api/prescriptions`

```json
{
  "visitId": "{{visitAId}}",
  "patientId": "{{patientAId}}",
  "doctorId": "{{doctorAId}}",
  "notes": "Take after meals",
  "items": [
    {
      "drugName": "Amoxicillin 500mg",
      "dosage": "500mg",
      "frequency": "3 times daily",
      "duration": "5 days",
      "route": "Oral",
      "instructions": "After food"
    },
    {
      "drugName": "Ibuprofen 400mg",
      "dosage": "400mg",
      "frequency": "Twice daily",
      "duration": "3 days",
      "route": "Oral",
      "instructions": "After food if pain persists"
    }
  ]
}
```

Expected:

- Status: `201`
- Save `data.id` as `prescriptionAId`.
- `data.items.length = 2`
- `data.patientName` and `data.doctorName` are not empty.

### 6.2 Prescription with no items

```json
{
  "visitId": "{{visitAId}}",
  "patientId": "{{patientAId}}",
  "doctorId": "{{doctorAId}}",
  "notes": "No items",
  "items": []
}
```

Expected:

- Status: `400`

### 6.3 Prescription item missing drug name

```json
{
  "visitId": "{{visitAId}}",
  "patientId": "{{patientAId}}",
  "doctorId": "{{doctorAId}}",
  "items": [
    {
      "drugName": "",
      "dosage": "500mg",
      "frequency": "Once daily",
      "duration": "3 days"
    }
  ]
}
```

Expected:

- Status: `400`

### 6.4 Get prescription

`GET /api/prescriptions/{{prescriptionAId}}`

Expected:

- Status: `200`
- Items are returned in order.

## 7. Billing Tests

Use Admin or Reception token.

### 7.1 Create paid cash invoice

`POST /api/billing/payments`

```json
{
  "visitId": "{{visitAId}}",
  "patientId": "{{patientAId}}",
  "totalAmount": 500,
  "discountAmount": 50,
  "discountPct": 10,
  "taxAmount": 0,
  "paidAmount": 450,
  "paymentMethod": 1,
  "notes": "Cash payment",
  "items": [
    {
      "serviceName": "Dental consultation",
      "serviceType": 1,
      "quantity": 1,
      "unitPrice": 500,
      "discountPct": 10
    }
  ]
}
```

Expected:

- Status: `201`
- `data.invoiceNumber` starts with `INV-`
- `data.status = Paid`
- `data.remainingAmount = 0`
- Save `data.id` as `paymentAId`.

### 7.2 Create partial payment

```json
{
  "visitId": "{{visitAId}}",
  "patientId": "{{patientAId}}",
  "totalAmount": 1000,
  "discountAmount": 0,
  "discountPct": 0,
  "taxAmount": 0,
  "paidAmount": 400,
  "paymentMethod": 2,
  "notes": "Partial card payment",
  "items": [
    {
      "serviceName": "Procedure",
      "serviceType": 4,
      "quantity": 1,
      "unitPrice": 1000,
      "discountPct": 0
    }
  ]
}
```

Expected:

- Status: `201`
- `data.status = Partial`
- `data.remainingAmount = 600`

### 7.3 Invalid payment item quantity

```json
{
  "visitId": "{{visitAId}}",
  "patientId": "{{patientAId}}",
  "totalAmount": 500,
  "discountAmount": 0,
  "discountPct": 0,
  "taxAmount": 0,
  "paidAmount": 500,
  "paymentMethod": 1,
  "items": [
    {
      "serviceName": "Bad item",
      "serviceType": 1,
      "quantity": 0,
      "unitPrice": 500,
      "discountPct": 0
    }
  ]
}
```

Expected:

- Status: `400`

### 7.4 Doctor cannot create payment

Use doctor token:

`POST /api/billing/payments`

Expected:

- Status: `403`

### 7.5 Daily revenue report

Use Admin token:

`GET /api/billing/reports/daily-revenue?date=2026-05-22`

Expected:

- Status: `200`
- `netRevenue` equals sum of paid amounts created today.
- `completedVisits` counts visits created today for the same tenant.

### 7.6 Reception cannot view daily revenue report

Use reception token:

`GET /api/billing/reports/daily-revenue?date=2026-05-22`

Expected:

- Status: `403`

## 8. Multi-Tenant Isolation Tests

These are the most important tests after the hardening work.

### 8.1 Tenant B cannot read Tenant A patient

Use `adminTokenB`:

`GET /api/patients/{{patientAId}}`

Expected:

- Status: `404`

### 8.2 Tenant B cannot update Tenant A patient

Use `adminTokenB`:

`PUT /api/patients/{{patientAId}}`

```json
{
  "fullName": "Hacked Name",
  "phoneNumber": "01012345670",
  "gender": 1
}
```

Expected:

- Status: `404`
- Re-read with Tenant A token and confirm name was not changed.

### 8.3 Tenant B cannot read Tenant A visit

Use `adminTokenB`:

`GET /api/visits/{{visitAId}}`

Expected:

- Status: `404`

### 8.4 Tenant B cannot read Tenant A prescription

Use `adminTokenB`:

`GET /api/prescriptions/{{prescriptionAId}}`

Expected:

- Status: `404`

### 8.5 Tenant B cannot change Tenant A appointment status

Use `adminTokenB`:

`PUT /api/appointments/{{appointmentAId}}/status`

```json
{
  "status": 4,
  "cancelReason": "Cross tenant attempt"
}
```

Expected:

- Status: `404`
- Re-read Tenant A daily appointments and confirm status was not changed by Tenant B.

### 8.6 Tenant B list endpoints do not contain Tenant A records

Use `adminTokenB`:

- `GET /api/patients`
- `GET /api/appointments/daily?date=2026-06-01`
- `GET /api/users`

Expected:

- Status: `200`
- No Tenant A patient, appointment, or user records.

## 9. Authorization Matrix

Run these endpoint checks with no token:

- `GET /api/patients` -> `401`
- `GET /api/users/me` -> `401`
- `POST /api/appointments` -> `401`
- `POST /api/visits` -> `401`
- `POST /api/billing/payments` -> `401`

Run these endpoint checks with Doctor token:

- `GET /api/patients` -> `200`
- `POST /api/patients` -> `200` or `201`
- `DELETE /api/patients/{{patientAId}}` -> `403`
- `GET /api/users` -> `403`
- `POST /api/billing/payments` -> `403`
- `GET /api/billing/reports/daily-revenue` -> `403`

Run these endpoint checks with Reception token:

- `GET /api/patients` -> `200`
- `POST /api/patients` -> `201`
- `POST /api/appointments` -> `201`
- `POST /api/visits` -> `403`
- `POST /api/prescriptions` -> `403`
- `POST /api/billing/payments` -> `201`
- `GET /api/billing/reports/daily-revenue` -> `403`

Run these endpoint checks with Admin token:

- All tenant-scoped endpoints above should pass except validation/business-rule failures.

## 10. Utility / Smoke Tests

### 10.1 DB connection

`GET /api/test/connection`

Expected:

- Status: `200` if `dbo.Drugs` exists.
- If `dbo.Drugs` does not exist, this endpoint fails. That indicates the DB schema is incomplete or the test endpoint is too tightly coupled to Drugs.

### 10.2 Weather sample endpoint

`GET /weatherforecast`

Expected:

- Status: `200`
- Returns 5 sample rows.
- This endpoint is not part of production clinic logic.

## 11. Recommended Execution Order

1. Apply `database/schema.sql` to an empty test DB if needed.
2. Run seed SQL from section 0.
3. Login Tenant A admin and Tenant B admin.
4. Create Tenant A doctor and reception.
5. Login doctor and reception.
6. Run patient tests.
7. Run appointment tests.
8. Run visit tests.
9. Run prescription tests.
10. Run billing tests.
11. Run multi-tenant isolation tests.
12. Run authorization matrix.

## 12. Pass / Fail Summary Checklist

Mark each item:

- Auth login/refresh/logout works.
- Role authorization returns `401` without token and `403` for wrong role.
- Patient CRUD works for same tenant.
- Duplicate patient phone returns `409`.
- Appointment conflict returns `409`.
- Appointment availability excludes booked slot.
- Visit creation updates linked appointment to completed.
- Prescription returns items and names.
- Billing returns invoice number, payment status, remaining amount.
- Daily revenue is tenant-scoped.
- Tenant B cannot read/update Tenant A patient.
- Tenant B cannot read Tenant A visit.
- Tenant B cannot read Tenant A prescription.
- Tenant B cannot update Tenant A appointment.
- Vulnerable package check is clean.
- Build succeeds when Visual Studio/API process is not locking DLLs, or succeeds with an alternate output folder.
