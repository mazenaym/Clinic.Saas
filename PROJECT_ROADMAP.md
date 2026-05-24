# ClinicFlow SaaS - Product Roadmap

آخر تحديث: 2026-05-23

## الهدف من الملف

ده ملف متابعة المشروع العملي. اعتبره لوحة تاسكات: إيه اتعمل، إيه ناقص، وإيه ترتيب التنفيذ من أول المنتج لحد الإطلاق.  
المشروع حاليا عنده Backend API قوي كبداية، لكن لسه محتاج Frontend كامل، اختبارات تلقائية، تجهيز Production، وخطة Product/Business أوضح.

## الحالة الحالية المختصرة

### تم إنجازه

- Backend solution مبني بـ Clean Architecture:
  - `Clinic.Saas.Domain`
  - `Clinic.Saas.Service`
  - `Clinic.Saas.Infrastructure`
  - `Clinic.Saas.api`
- Database schema موجود في `database/schema.sql`.
- Authentication بـ JWT و Refresh Tokens.
- Multi-tenancy عن طريق TenantId و Subdomain/JWT.
- Role-based authorization:
  - SuperAdmin
  - Admin
  - Doctor
  - Reception
- Modules منفذة كـ API:
  - Auth
  - Onboarding
  - Platform Admin
  - Users
  - Patients
  - Appointments
  - Visits
  - Prescriptions
  - Billing
- Validators باستخدام FluentValidation.
- Repositories باستخدام Dapper.
- Testing map يدوي موجود في `TESTING_MAP.md`.
- المشروع يعمل build بنجاح بدون أخطاء.

### لم يتم إنجازه بعد

- لا يوجد Frontend فعلي داخل الريبو حتى الآن.
- لا يوجد Test Project تلقائي Unit/Integration.
- لا يوجد Docker/Docker Compose.
- لا يوجد CI/CD.
- لا يوجد PDF generation للروشتات والفواتير.
- لا يوجد WhatsApp/SMS/Email integration فعلي.
- لا يوجد Inventory API مكتمل رغم وجود Entities/DTOs.
- لا يوجد Lab Requests API مكتمل رغم وجود Entities/DTOs.
- لا يوجد Patient Portal.
- لا يوجد Reports module شامل، الموجود حاليا تقرير إيراد يومي فقط.
- لا يوجد Audit Logging فعلي كامل.
- لا يوجد production monitoring/logging setup.

## خريطة المنتج غير التقنية

### المشكلة التي نحلها

العيادات الصغيرة والمتوسطة بتضيع وقت في:

- ملفات المرضى الورقية.
- تعارض المواعيد.
- متابعة الفواتير والمدفوعات.
- كتابة الروشتات بشكل غير منظم.
- صعوبة معرفة أداء العيادة ماليا وتشغيليا.

### المستخدمون الأساسيون

- صاحب المنصة: يدير العيادات والاشتراكات.
- مدير العيادة: يدير المستخدمين، التقارير، الإعدادات.
- الدكتور: يشوف المرضى، يسجل الكشف، يكتب الروشتة.
- الريسبشن: يسجل المرضى، يحجز المواعيد، يحصل المدفوعات.
- المريض: لاحقا يحجز أونلاين ويشوف روشتاته ومواعيده.

### MVP الحقيقي

أول نسخة قابلة للبيع لازم تغطي:

- تسجيل عيادة جديدة.
- تسجيل دخول وصلاحيات.
- إضافة مستخدمين للعيادة.
- إدارة المرضى.
- إدارة المواعيد.
- تسجيل كشف.
- كتابة روشتة.
- تسجيل فاتورة ودفع.
- Dashboard بسيط للعيادة.
- واجهة عربية سهلة الاستخدام.

## Roadmap عام

## المرحلة 0 - تثبيت الأساس الحالي

الهدف: نخلي اللي موجود موثوق ومفهوم قبل بناء الواجهة.

- [x] Clean Architecture.
- [x] SQL schema.
- [x] JWT Auth.
- [x] Tenant isolation مبدئي.
- [x] Basic controllers/use cases.
- [x] Manual testing map.
- [x] Build ناجح.
- [ ] مراجعة كل endpoint مع `TESTING_MAP.md`.
- [ ] تحويل أهم الاختبارات اليدوية لاختبارات تلقائية.
- [ ] توحيد response format في كل الـ endpoints.
- [ ] توثيق API النهائي في Swagger/Scalar.
- [ ] تنظيف README لأن النص العربي ظاهر Encoding غير سليم.

## المرحلة 1 - Backend MVP Hardening

الهدف: API يبقى جاهز للفرونت وبدون مفاجآت.

### Auth & Security

- [x] Login.
- [x] Refresh token.
- [x] Logout.
- [x] Role policies.
- [ ] Account lockout بشكل مؤكد ومختبر.
- [ ] Password change/reset.
- [ ] SuperAdmin bootstrap مؤمن بشكل أفضل للإنتاج.
- [ ] Rate limiting.
- [ ] CORS production policy.
- [ ] Audit log لكل عمليات CRUD المهمة.

### Tenants & Onboarding

- [x] Check subdomain.
- [x] Register clinic.
- [x] Create clinic من SuperAdmin.
- [x] Activate/deactivate clinic.
- [x] Subscriptions table/use cases.
- [ ] Trial period flow.
- [ ] Plan limits: عدد مستخدمين، عدد مرضى، مميزات حسب الخطة.
- [ ] Billing state للعيادة: active/trial/expired/suspended.
- [ ] Tenant settings endpoint كامل.

### Users

- [x] Create user.
- [x] List users.
- [x] Current user.
- [ ] Update user.
- [ ] Deactivate user.
- [ ] Reset user password.
- [ ] User profile/preferences.
- [ ] منع حذف/تعطيل آخر Admin في العيادة.

### Patients

- [x] Create patient.
- [x] List patients.
- [x] Search patients.
- [x] Get patient details.
- [x] Update patient.
- [x] Soft delete.
- [ ] Patient timeline موحد: زيارات، مواعيد، روشتات، مدفوعات.
- [ ] Import/export Excel.
- [ ] Patient documents upload.
- [ ] Duplicate detection أقوى بالهاتف/الرقم القومي.

### Appointments

- [x] Create appointment.
- [x] Daily appointments.
- [x] Availability.
- [x] Conflict detection.
- [x] Update status.
- [ ] Weekly/monthly calendar endpoints.
- [ ] Reschedule appointment.
- [ ] Cancellation reason/reporting.
- [ ] Online booking approval flow.
- [ ] Reminder jobs.

### Visits

- [x] Create visit.
- [x] Get visit by id.
- [x] Link visit to appointment.
- [ ] Update visit.
- [ ] Visit history by patient.
- [ ] Clinical templates.
- [ ] Follow-up appointment auto-create.
- [ ] Lock/finalize visit after completion.

### Prescriptions

- [x] Create prescription.
- [x] Get prescription.
- [x] Prescription items.
- [ ] Prescription PDF.
- [ ] Print layout.
- [ ] Drug autocomplete/source.
- [ ] WhatsApp send.
- [ ] QR code verification.
- [ ] Drug interaction warnings.

### Billing

- [x] Create payment/invoice.
- [x] Payment items.
- [x] Paid/partial status calculation.
- [x] Daily revenue report.
- [ ] Get payment/invoice by id.
- [ ] List patient payments.
- [ ] Update/refund payment.
- [ ] Receipt/PDF invoice.
- [ ] Debt tracking.
- [ ] Monthly revenue report.

### Admin Dashboard

- [x] Platform dashboard.
- [x] Clinics list.
- [x] Clinic details.
- [x] Subscription creation.
- [ ] Clinic usage metrics.
- [ ] Revenue from subscriptions.
- [ ] Expiring subscriptions alerts.
- [ ] Admin activity log.

## المرحلة 2 - Frontend MVP

الهدف: تحويل الـ API لمنتج فعلي قابل للاستخدام.

### Setup

- [ ] إنشاء Angular app.
- [ ] اختيار UI framework مناسب.
- [ ] RTL + Arabic-first layout.
- [ ] Auth service.
- [ ] JWT interceptor.
- [ ] Route guards.
- [ ] Role-based menu.
- [ ] Shared API client.
- [ ] Error/toast handling.

### Layout

- [ ] Login page.
- [ ] Sidebar/Header.
- [ ] Clinic switch/context display.
- [ ] Dashboard shell.
- [ ] Loading/empty/error states.
- [ ] Responsive layout للتابلت واللاب.

### Clinic Dashboard

- [ ] Today's appointments.
- [ ] New patients count.
- [ ] Revenue today.
- [ ] Pending payments.
- [ ] Quick actions:
  - Add patient
  - Book appointment
  - Start visit
  - Create invoice

### Patients UI

- [ ] Patients list.
- [ ] Search/filter.
- [ ] Add patient form.
- [ ] Edit patient form.
- [ ] Patient profile.
- [ ] Patient timeline.
- [ ] Allergy warning display.

### Appointments UI

- [ ] Daily calendar.
- [ ] Book appointment modal/page.
- [ ] Availability picker.
- [ ] Status changes.
- [ ] Doctor filter.
- [ ] Conflict error handling.

### Visits UI

- [ ] Doctor queue.
- [ ] Visit form.
- [ ] Vital signs.
- [ ] Diagnosis/notes.
- [ ] Follow-up date.
- [ ] Link prescription.

### Prescriptions UI

- [ ] Prescription builder.
- [ ] Add/remove medicines.
- [ ] Dosage/frequency/duration fields.
- [ ] Print preview.
- [ ] PDF download once backend supports it.

### Billing UI

- [ ] Create invoice/payment.
- [ ] Payment item editor.
- [ ] Paid/remaining summary.
- [ ] Daily revenue view.
- [ ] Patient debt display.

### Platform Admin UI

- [ ] SuperAdmin login.
- [ ] Platform dashboard.
- [ ] Clinics table.
- [ ] Create/edit clinic.
- [ ] Activate/suspend clinic.
- [ ] Add subscription.

## المرحلة 3 - Testing & Quality

الهدف: كل تغيير جديد ما يكسرش القديم.

- [ ] إنشاء `Clinic.Saas.Tests`.
- [ ] Unit tests للـ validators.
- [ ] Unit tests للـ use cases المهمة.
- [ ] Integration tests للـ API مع test database.
- [ ] Tenant isolation automated tests.
- [ ] Authorization matrix automated tests.
- [ ] Seed/test data scripts.
- [ ] Frontend component tests.
- [ ] E2E tests لأهم flows:
  - Login
  - Add patient
  - Book appointment
  - Start visit
  - Create prescription
  - Create payment
- [ ] Vulnerability scan في CI.

## المرحلة 4 - Production Readiness

الهدف: المشروع يبقى قابل للنشر والاستخدام الحقيقي.

- [ ] Dockerfile للـ API.
- [ ] Docker Compose للـ API + SQL Server.
- [ ] Environment variables بدل secrets في appsettings.
- [ ] Database migrations/versioning.
- [ ] Structured logging بـ Serilog.
- [ ] Health checks.
- [ ] Error handling middleware.
- [ ] Request correlation id.
- [ ] Backup strategy.
- [ ] Deployment guide.
- [ ] GitHub Actions:
  - build
  - test
  - security scan
  - deploy

## المرحلة 5 - Growth Features

بعد MVP شغال ومستخدمين يجربوه.

- [ ] SMS reminders.
- [ ] WhatsApp prescription sending.
- [ ] Online booking portal.
- [ ] Patient portal.
- [ ] Inventory management.
- [ ] Lab requests.
- [ ] Advanced reports.
- [ ] Excel/PDF exports.
- [ ] Multi-branch clinics.
- [ ] White label branding.

## المرحلة 6 - Smart/AI Features

دي مميزات لاحقة، لا تبدأ قبل ما الـ MVP يبقى مستقر.

- [ ] Voice-to-text أثناء الكشف.
- [ ] AI clinical note summarization.
- [ ] Diagnosis suggestions.
- [ ] Drug interaction checks.
- [ ] Predictive analytics للزيارات والإيرادات.
- [ ] Smart reminders للمرضى المزمنين.

## ترتيب التنفيذ المقترح من الآن

### Sprint 1 - تثبيت الـ API

- [ ] تشغيل كل سيناريوهات `TESTING_MAP.md`.
- [ ] إصلاح أي endpoint يفشل.
- [ ] إضافة Integration test project.
- [ ] تغطية Auth + Tenant isolation + Patients.
- [ ] توثيق endpoints النهائي.

### Sprint 2 - بداية الفرونت

- [ ] إنشاء Angular app.
- [ ] Login + token storage + interceptor.
- [ ] Main layout.
- [ ] Patients list/create/edit.
- [ ] ربط كامل مع API.

### Sprint 3 - تشغيل العيادة يوميا

- [ ] Appointments calendar.
- [ ] Book appointment.
- [ ] Doctor queue.
- [ ] Create visit.
- [ ] Patient profile timeline.

### Sprint 4 - الروشتة والفواتير

- [ ] Prescription UI.
- [ ] Billing UI.
- [ ] Daily revenue UI.
- [ ] PDF endpoints للروشتة والفاتورة.

### Sprint 5 - Admin & SaaS

- [ ] Platform admin UI.
- [ ] Clinic onboarding UI.
- [ ] Subscription management.
- [ ] Plan limits.
- [ ] Clinic suspension/expiry behavior.

### Sprint 6 - Release Candidate

- [ ] Docker.
- [ ] CI/CD.
- [ ] Production config.
- [ ] Error logging.
- [ ] Final E2E tests.
- [ ] Demo data.
- [ ] Pilot deployment لأول عيادة.

## Definition of Done لأي Task

أي task لا تعتبر خلصت إلا لما:

- الكود يعمل build.
- الـ endpoint أو الشاشة متجربة.
- الحالات الفاشلة متغطية: validation/auth/not found/conflict.
- Tenant isolation متأكد لو البيانات تخص عيادة.
- UI فيه loading/error/empty state لو task فرونت.
- مضاف أو محدث اختبار تلقائي لو التغيير حساس.
- التوثيق اتحدث لو الـ API أو flow اتغير.

## أولويات المنتج

### Critical

- Auth.
- Tenant isolation.
- Patients.
- Appointments.
- Visits.
- Billing.
- Basic dashboard.
- Frontend usable.

### High

- Prescriptions.
- PDF.
- Reports.
- Admin SaaS management.
- Tests.
- Deployment.

### Medium

- SMS/WhatsApp.
- Online booking.
- Inventory.
- Lab requests.
- Patient portal.

### Later

- AI.
- Voice.
- Telemedicine.
- Predictive analytics.
- Multi-branch advanced features.

## ملاحظات مهمة

- المشروع حاليا API-first، فلا تبدأ في مميزات AI أو WhatsApp قبل وجود واجهة مستخدم MVP.
- أهم خطر في المشروع هو التوهان بين "نظام عيادة" و"SaaS platform". الحل: نبني مسار العيادة اليومي الأول، وبعده إدارة الاشتراكات والمنصة.
- أهم شيء لازم يفضل محمي طول الوقت: Tenant isolation. أي API يرجع أو يعدل بيانات لازم يتأكد إنها تخص نفس العيادة.
- الفرونت هو أكبر قطعة ناقصة حاليا، ومن غيره المشروع سيظل تقنيا جيد لكنه غير قابل للبيع أو التجربة من مستخدم حقيقي.
