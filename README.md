
ClinicFlow SaaS
نظام إدارة العيادات الذكي
Software Requirements & Architecture Documentation

Version	1.0.0 — MVP
Stack	.NET 10 · Angular · SQLServer
Date	April 2026
 
1. Executive Summary — نظرة عامة

ClinicFlow هو نظام SaaS متكامل لإدارة العيادات الطبية، مصمم لاستبدال الأنظمة الورقية والعشوائية بمنصة رقمية ذكية وسريعة. الهدف الأساسي هو تمكين الأطباء والعيادات من إدارة مرضاهم، مواعيدهم، وأموالهم في مكان واحد.

مشكلة السوق
المشكلة الحالية	الحل الذي يقدمه ClinicFlow
ضياع ملفات المرضى الورقية	ملف رقمي كامل لكل مريض مع بحث فوري
تعارض المواعيد وعدم التنظيم	نظام حجز ذكي يمنع التعارض تلقائياً
صعوبة متابعة الإيرادات	تقارير مالية لحظية يومية وشهرية وسنوية
الروشتات غير المقروءة	روشتة إلكترونية واضحة قابلة للطباعة والإرسال
لا يوجد نسخ احتياطي للبيانات	Cloud backup تلقائي ومشفر بالكامل

قيمة النظام Value Proposition
•	توفير 2-3 ساعات يومياً من الوقت الضائع في البحث والتنظيم
•	تقليل الأخطاء الطبية الناتجة عن فقدان معلومات المريض
•	زيادة رضا المرضى بنسبة تصل إلى 40% بسبب تنظيم المواعيد
•	نمو الإيرادات من خلال متابعة المدفوعات والديون
 
2. System Modules — مكونات النظام

2.1 إدارة المرضى — Patients Module

الجزء الأساسي في النظام. كل المعلومات المتعلقة بالمريض تُخزَّن هنا وتُسترجع بشكل فوري.

البيانات	الوصف	ملاحظات
FullName	الاسم الكامل للمريض	مطلوب، بحث Full-text
PhoneNumber	رقم الموبايل	مطلوب، Unique
DateOfBirth	تاريخ الميلاد	يُحسب السن تلقائياً
Gender	النوع	Enum: Male/Female
MedicalHistory	التاريخ المرضي	JSON / Text لمرونة أكبر
DrugAllergies	حساسية الأدوية	Critical — يُعرض دائماً
BloodType	فصيلة الدم	Enum: A+/A-/B+/B-/O+/O-/AB+/AB-
Notes	ملاحظات إضافية	اختياري
PatientCode	كود المريض	Auto-generated: CLN-00001

المميزات الوظيفية
•	بحث فوري بالاسم أو رقم الموبايل أو كود المريض
•	عرض كامل لسجل الزيارات (Visit History) مرتباً زمنياً
•	تنبيه تلقائي عند وجود حساسية أدوية خلال الكشف
•	استيراد/تصدير بيانات المرضى بصيغة Excel

2.2 إدارة المواعيد — Appointments Module

الميزة	التفاصيل
Booking	حجز موعد مع تحديد الوقت والمدة
Conflict Detection	منع حجز وقت محجوز مسبقاً تلقائياً
Status Management	Scheduled / Confirmed / Completed / Cancelled / No-Show
Calendar View	عرض يومي وأسبوعي وشهري
SMS Reminder	إشعار تلقائي قبل الموعد بساعة و24 ساعة
Online Booking	حجز ذاتي من المريض عبر الرابط الخاص بالعيادة
Walk-in Support	إضافة مريض مباشر بدون موعد مسبق

2.3 الكشف والزيارات — Visits / Consultations Module

أهم وأعقد module في النظام. يُسجِّل كل تفاصيل الكشف الطبي.

المرحلة	البيانات المُسجَّلة	التقنية المستخدمة
Chief Complaint	الشكوى الرئيسية للمريض	Free text + Templates
Vital Signs	ضغط / حرارة / وزن / نبض	Structured fields
Examination	نتائج الفحص الإكلينيكي	Rich text editor
Diagnosis	التشخيص (ICD-10)	Searchable ICD-10 DB
Prescription	الروشتة	Sub-module كامل
Lab Requests	طلبات التحاليل	Checkbox list + Custom
Follow-up Date	موعد المتابعة	Auto-creates appointment

ميزات متقدمة — Creative Features
•	Voice-to-Text: الدكتور يتكلم والنظام يكتب تلقائياً (Web Speech API + Whisper AI)
•	Clinical Templates: قوالب جاهزة لكل تخصص (باطنة / أطفال / جلدية / عظام)
•	Smart Diagnosis Suggestions: اقتراح تشخيصات بناءً على الأعراض المُدخَلة (AI)
•	Auto Drug Interaction Check: تحقق تلقائي من تعارض الأدوية عند كتابة الروشتة

2.4 الروشتة الإلكترونية — E-Prescription Module

•	قاعدة بيانات أدوية شاملة مع Autocomplete
•	تحديد الجرعة والمدة وطريقة الاستخدام لكل دواء
•	طباعة الروشتة بشعار العيادة وبيانات الدكتور
•	إرسال الروشتة PDF مباشرة عبر WhatsApp
•	QR Code على الروشتة للتحقق من صحتها
•	تاريخ كامل لجميع الروشتات السابقة للمريض

2.5 الفواتير والمدفوعات — Billing Module

الوظيفة	التفاصيل
Invoice Generation	فاتورة تلقائية بعد كل كشف
Service Items	إضافة خدمات: كشف / أشعة / تحاليل / إجراءات
Payment Methods	كاش / كارت / تحويل بنكي / تأمين صحي
Partial Payments	دفع جزئي مع تتبع المتبقي
Insurance Support	ربط مع التأمين الصحي
Discount Management	خصومات مرنة بالنسبة أو المبلغ
Daily Revenue Report	تقرير الإيراد اليومي مع تفصيل كل معاملة

2.6 إدارة المستخدمين والصلاحيات — Users & Roles Module

الصلاحية	Doctor	Reception	Admin
إضافة/تعديل مريض	✅	✅	✅
تسجيل الكشف	✅	❌	✅
كتابة الروشتة	✅	❌	❌
إدارة المواعيد	✅	✅	✅
إدارة الفواتير	👁 View	✅	✅
التقارير المالية	❌	❌	✅
إدارة المستخدمين	❌	❌	✅
إعدادات النظام	❌	❌	✅

2.7 التقارير — Reports Module

•	تقرير المرضى: عدد المرضى الجدد / الإجمالي / حسب التخصص
•	تقرير المواعيد: نسبة الحضور / الغياب / الإلغاء
•	تقرير الإيرادات: يومي / أسبوعي / شهري / سنوي مع رسم بياني
•	تقرير الأدوية الأكثر وصفاً (Prescription Analytics)
•	تقرير أداء الدكتور: عدد الكشوفات / متوسط وقت الكشف
•	Export: تصدير كل التقارير بصيغة PDF أو Excel

2.8 إدارة المخزون — Inventory Module

•	تسجيل الأدوية والمستلزمات مع الكميات
•	تنبيه تلقائي عند وصول الكمية للحد الأدنى
•	تتبع تواريخ الصلاحية مع تنبيه مسبق بـ 30 يوم
•	ربط الصرف بالروشتة لخصم تلقائي من المخزون

 
3. Technical Architecture — البنية التقنية

3.1 Technology Stack

Layer	Technology	الاختيار والسبب
Backend	.NET 10 (ASP.NET Core Web API)	أداء عالٍ، Type Safety، دعم ممتاز من Microsoft
Architecture	Clean Architecture	Separation of Concerns، سهولة الاختبار والتوسع
ORM / Data Access	Dapper + Unit of Work	أداء أسرع من EF Core، تحكم كامل في SQL
Database Strategy	Database First	تصميم DB بدقة ثم Generate Models
Validation	FluentValidation	قواعد Validation واضحة ومنفصلة عن Business Logic
Frontend	Angular 18	Enterprise-grade، Two-way binding، RxJS
Database	PostgreSQL 16	مفتوح المصدر، أداء ممتاز، JSON support
Cache	Redis	تسريع الاستعلامات المتكررة والـ Sessions
Auth	ASP.NET Identity + JWT	Secure، Standard، قابل للتوسع
Hosting	AWS / DigitalOcean	Docker Containers + CI/CD Pipeline
Realtime	SignalR	إشعارات فورية وتحديثات Live
PDF Generation	QuestPDF	مكتبة .NET لـ PDF عالية الجودة
SMS	Twilio / Vonage	إرسال رسائل SMS للمواعيد

3.2 Clean Architecture — طبقات النظام

النظام مبني على Clean Architecture بأربع طبقات واضحة ومنفصلة:

Layer	المحتوى	الاعتماديات
Domain Layer	Entities, Enums, Interfaces, Domain Events	لا تعتمد على أي layer أخرى
Application Layer	Use Cases, DTOs, Commands, Queries, Validators	تعتمد على Domain فقط
Infrastructure Layer	Repositories, Dapper, Email, SMS, File Storage	تعتمد على Application + Domain
Presentation Layer	API Controllers, Middleware, Filters	تعتمد على Application فقط

مثال تطبيقي: تسجيل كشف جديد
•	Controller يستقبل CreateVisitCommand
•	FluentValidation يتحقق من صحة البيانات
•	Application Layer تُنفِّذ Business Logic
•	Unit of Work يبدأ Transaction
•	Dapper ينفذ INSERT على الـ Database
•	Unit of Work يُكمل Transaction
•	Domain Event يُطلَق: VisitCreated
•	Notification Handler يرسل SMS للمريض

3.3 Unit of Work Pattern

كل العمليات المترابطة تتم داخل Transaction واحدة لضمان Consistency:

Interface	Implementation
IUnitOfWork	UnitOfWork (Dapper + IDbConnection)
IPatientRepository	PatientRepository : IPatientRepository
IAppointmentRepository	AppointmentRepository
IVisitRepository	VisitRepository
IPrescriptionRepository	PrescriptionRepository
IPaymentRepository	PaymentRepository

 
4. Database Design — تصميم قاعدة البيانات

النظام يعتمد Database First Approach — يتم تصميم الـ Database Schema أولاً بدقة، ثم يُبنى فوقها التطبيق.

4.1 Core Tables — الجداول الأساسية

Patients Table
Column	Type	Constraints	Description
Id	UUID	PK, Default gen()	Primary Key
PatientCode	VARCHAR(20)	UNIQUE, NOT NULL	CLN-00001
FullName	VARCHAR(200)	NOT NULL	اسم المريض
PhoneNumber	VARCHAR(20)	UNIQUE, NOT NULL	رقم الموبايل
DateOfBirth	DATE	NULL	تاريخ الميلاد
Gender	SMALLINT	NOT NULL	1=Male, 2=Female
BloodType	VARCHAR(5)	NULL	A+, B-, O+, ...
MedicalHistory	TEXT	NULL	Free text
DrugAllergies	TEXT	NULL	Critical field
IsActive	BOOLEAN	DEFAULT true	Soft Delete
TenantId	UUID	FK → Tenants	Multi-tenancy
CreatedAt	TIMESTAMP	DEFAULT NOW()	Audit
CreatedBy	UUID	FK → Users	Audit

Appointments Table
Column	Type	Constraints	Description
Id	UUID	PK	Primary Key
PatientId	UUID	FK → Patients	المريض
DoctorId	UUID	FK → Users	الدكتور
AppointmentDate	DATE	NOT NULL	تاريخ الموعد
StartTime	TIME	NOT NULL	وقت البداية
EndTime	TIME	NOT NULL	وقت النهاية
Status	SMALLINT	NOT NULL	1=Scheduled, 2=Confirmed, 3=Completed, 4=Cancelled, 5=NoShow
Type	SMALLINT	NOT NULL	1=New, 2=Follow-up, 3=Emergency
Notes	TEXT	NULL	ملاحظات
ReminderSent	BOOLEAN	DEFAULT false	SMS Reminder
TenantId	UUID	FK → Tenants	Multi-tenancy

Visits Table
Column	Type	Constraints	Description
Id	UUID	PK	Primary Key
PatientId	UUID	FK → Patients	المريض
AppointmentId	UUID	FK → Appointments, NULL	قد يكون Walk-in
DoctorId	UUID	FK → Users	الدكتور
VisitDate	TIMESTAMP	NOT NULL	تاريخ ووقت الكشف
ChiefComplaint	TEXT	NOT NULL	الشكوى الرئيسية
VitalSigns	JSONB	NULL	{bp, temp, weight, pulse}
ClinicalNotes	TEXT	NULL	نتائج الفحص
Diagnosis	TEXT	NULL	التشخيص
DiagnosisCode	VARCHAR(20)	NULL	ICD-10 Code
FollowUpDate	DATE	NULL	موعد المتابعة
TenantId	UUID	FK → Tenants	Multi-tenancy

Prescriptions + PrescriptionItems Tables
Column	Type	Constraints	Description
Id	UUID	PK	Primary Key (Prescriptions)
VisitId	UUID	FK → Visits	الكشف المرتبط
PatientId	UUID	FK → Patients	المريض
Notes	TEXT	NULL	ملاحظات الروشتة
— PrescriptionItems —	—	—	—
PrescriptionId	UUID	FK → Prescriptions	الروشتة
DrugId	UUID	FK → Drugs	الدواء
Dosage	VARCHAR(100)	NOT NULL	الجرعة: 500mg
Frequency	VARCHAR(100)	NOT NULL	مرتين يومياً
Duration	VARCHAR(100)	NOT NULL	لمدة 7 أيام
Instructions	TEXT	NULL	بعد الأكل...

Payments Table
Column	Type	Constraints	Description
Id	UUID	PK	Primary Key
VisitId	UUID	FK → Visits	الكشف
PatientId	UUID	FK → Patients	المريض
TotalAmount	DECIMAL(10,2)	NOT NULL	إجمالي الفاتورة
PaidAmount	DECIMAL(10,2)	NOT NULL	المبلغ المدفوع
RemainingAmount	DECIMAL(10,2)	GENERATED	TotalAmount - PaidAmount
PaymentMethod	SMALLINT	NOT NULL	1=Cash, 2=Card, 3=Transfer, 4=Insurance
Status	SMALLINT	NOT NULL	1=Pending, 2=Partial, 3=Paid, 4=Refunded
DiscountAmount	DECIMAL(10,2)	DEFAULT 0	قيمة الخصم
TenantId	UUID	FK → Tenants	Multi-tenancy

4.2 Multi-Tenancy Design

النظام يدعم Multi-Tenancy بنمط Shared Database مع TenantId على كل جدول:

Pattern	التفاصيل
Tenants Table	كل عيادة = Tenant مستقل بـ Subdomain خاص
Row-Level Security	كل استعلام Dapper يُضاف عليه WHERE TenantId = @TenantId
Tenant Middleware	يُحدد TenantId من الـ JWT Token أو Subdomain تلقائياً
Data Isolation	بيانات كل عيادة معزولة تماماً عن باقي العيادات

 
5. API Design — تصميم الـ API

5.1 Patients API

Method	Endpoint	Description	Auth
GET	/api/patients	قائمة المرضى (Paginated)	Reception+
GET	/api/patients/{id}	تفاصيل مريض	Reception+
GET	/api/patients/search?q=	بحث بالاسم/الموبايل	Reception+
POST	/api/patients	إضافة مريض جديد	Reception+
PUT	/api/patients/{id}	تعديل بيانات مريض	Reception+
DELETE	/api/patients/{id}	حذف ناعم (Soft Delete)	Admin
GET	/api/patients/{id}/visits	سجل زيارات المريض	Doctor+

5.2 Appointments API

Method	Endpoint	Description	Auth
GET	/api/appointments	جدول المواعيد	Reception+
GET	/api/appointments/today	مواعيد اليوم	Reception+
GET	/api/appointments/availability	الأوقات المتاحة	Public
POST	/api/appointments	حجز موعد	Reception+
PUT	/api/appointments/{id}/status	تغيير حالة الموعد	Reception+
DELETE	/api/appointments/{id}	إلغاء موعد	Reception+

5.3 Visits & Prescriptions API

Method	Endpoint	Description	Auth
POST	/api/visits	بدء كشف جديد	Doctor
PUT	/api/visits/{id}	تحديث الكشف	Doctor
GET	/api/visits/{id}	تفاصيل الكشف	Doctor+
POST	/api/visits/{id}/prescriptions	إضافة روشتة	Doctor
GET	/api/prescriptions/{id}/pdf	تحميل PDF الروشتة	Doctor+
POST	/api/prescriptions/{id}/send-whatsapp	إرسال WhatsApp	Doctor+

5.4 Standard Response Format

Success Response	{ "success": true, "data": {...}, "message": "تم بنجاح", "statusCode": 200 }

Error Response	{ "success": false, "errors": [...], "message": "خطأ في البيانات", "statusCode": 400 }

Paginated Response	{ "data": [...], "page": 1, "pageSize": 20, "totalCount": 150, "totalPages": 8 }

 
6. Security — الأمان

البيانات الطبية من أكثر البيانات حساسية وتخضع لقوانين صارمة. الأمان هو الأولوية الأولى.

6.1 Authentication & Authorization

•	JWT Tokens مع Refresh Token Strategy
•	Token Expiry قصير (15 دقيقة) مع Refresh Token (7 أيام)
•	Role-Based Access Control (RBAC) على مستوى Controller و Action
•	Policy-Based Authorization للصلاحيات المعقدة
•	Account Lockout بعد 5 محاولات دخول فاشلة

6.2 Data Security

الإجراء	التفاصيل
Encryption at Rest	تشفير بيانات حساسة في DB بـ AES-256
Encryption in Transit	HTTPS / TLS 1.3 على كل الـ Endpoints
SQL Injection Prevention	Dapper Parameterized Queries — صفر Concatenation
Audit Logging	كل CRUD Operation تُسجَّل مع UserId + Timestamp
Soft Delete	البيانات لا تُحذف فعلياً — IsDeleted Flag فقط
CORS Policy	قائمة بيضاء محددة من الـ Origins المسموحة فقط
Rate Limiting	حماية من Brute Force: 100 طلب/دقيقة لكل IP
GDPR Compliance	حق المريض في حذف/تصدير بياناته الشخصية

 
7. User Flows — سيناريوهات الاستخدام

7.1 سيناريو مريض جديد — Complete Flow

Step	المسؤول	الإجراء
1	Reception	المريض يدخل → الريسبشن يبحث بالموبايل
2	Reception	مريض جديد → إضافة بيانات كاملة
3	Reception	حجز موعد أو إضافة Walk-in فوري
4	System	SMS تأكيد للمريض تلقائياً
5	Doctor	الدكتور يرى المريض في قائمته → يفتح الكشف
6	Doctor	يُسجل الشكوى والأعراض ونتائج الفحص
7	Doctor	يختار التشخيص من ICD-10 أو يكتبه
8	Doctor	يكتب الروشتة مع التحقق من التعارض
9	Doctor	يحدد موعد متابعة (يُحجز تلقائياً)
10	Reception	إنشاء فاتورة وتسجيل الدفع
11	System	إرسال الروشتة PDF للمريض عبر WhatsApp
12	System	تحديث كل الـ Reports والـ Dashboard

7.2 سيناريو الحجز الأونلاين

•	المريض يدخل رابط العيادة: clinic.clinicflow.com/dr-ahmed
•	يختار التخصص والدكتور والتاريخ المناسب
•	يرى الأوقات المتاحة فقط ويختار
•	يُدخل بياناته (أو يسجل دخول إن كان مريضاً قديماً)
•	يتلقى SMS + بريد إلكتروني بتأكيد الموعد
•	تذكير تلقائي قبل 24 ساعة وقبل ساعة من الموعد

 
8. MVP Plan & Roadmap

8.1 Phase 1 — MVP (8 أسابيع)

الأسبوع	المهام	الأولوية
1-2	Setup: Clean Arch + DB Schema + Auth + Multi-tenancy	🔴 Critical
3	Patients Module: CRUD + Search + Medical History	🔴 Critical
4	Appointments Module: Booking + Calendar + Conflict Detection	🔴 Critical
5	Visits Module: Consultation Form + Templates	🔴 Critical
6	Prescriptions: E-Prescription + PDF Generation	🟡 High
7	Billing: Invoice + Payments + Basic Reports	🟡 High
8	Testing + Bug Fixes + Deployment + Onboarding	🔴 Critical

8.2 Phase 2 — Growth (الشهر 3-4)

•	SMS Notifications Integration (Twilio/Vonage)
•	WhatsApp Integration للروشتات والتذكيرات
•	Online Booking Portal للمرضى
•	Advanced Reports + Analytics Dashboard
•	Inventory Management Module

8.3 Phase 3 — Scale (الشهر 5-6)

•	Mobile App (Angular PWA أو Flutter)
•	Voice-to-Text Integration (Whisper AI)
•	AI Smart Diagnosis Suggestions
•	Insurance Integration
•	Multi-branch Support
•	Telemedicine (Video Consultation)

 
9. Creative Differentiators — ما يميزك

هذه الأفكار الإبداعية ستجعل ClinicFlow مختلفاً جذرياً عن المنافسين:

الميزة	التفاصيل والتقنية
🎤 Voice-to-Text للدكتور	الدكتور يتكلم أثناء الكشف → النظام يكتب تلقائياً بالعربي والإنجليزي (Whisper AI API)
🤖 AI Diagnosis Assistant	بناءً على الأعراض المُدخَلة يقترح النظام تشخيصات محتملة (Claude AI API)
💊 Drug Interaction Check	تحقق فوري من تعارض الأدوية لحظة كتابة الروشتة
📱 WhatsApp Native Integration	إرسال الروشتة والمواعيد والتذكيرات مباشرة عبر WhatsApp Business API
📊 Smart Dashboard	Dashboard ذكي يُظهر Insights تلقائية: 'إيراداتك انخفضت 20% هذا الأسبوع'
🌐 Patient Portal	كل مريض له حساب يرى زياراته وروشتاته ويحجز مواعيده بنفسه
📄 QR Code Prescriptions	QR Code على كل روشتة يتحقق من صحتها ويُظهر التفاصيل للصيدلي
🔔 Smart Reminders	تذكيرات ذكية: متابعة المرضى المزمنين / تجديد الأدوية / الفحوصات الدورية
📈 Predictive Analytics	توقع أوقات الذروة وعدد المرضى بناءً على البيانات التاريخية
🎨 White-Label Solution	كل عيادة تحصل على Subdomain وشعار وألوان خاصة بها
💬 In-App Messaging	رسائل مشفرة بين الدكتور والريسبشن داخل التطبيق
📷 Document Scanner	مسح مستندات المرضى (تحاليل / أشعة) مباشرة من الكاميرا

 
10. Business Model — نموذج الأعمال

10.1 Pricing Plans

	Starter	Professional	Enterprise
السعر/شهر	299 جنيه	599 جنيه	1,200 جنيه+
المستخدمون	2 users	5 users	غير محدود
المرضى/شهر	200 مريض	1,000 مريض	غير محدود
SMS	❌	100 رسالة	غير محدود
WhatsApp	❌	✅	✅
Online Booking	❌	✅	✅
AI Features	❌	❌	✅
Dedicated Support	❌	❌	✅

10.2 الإيرادات المتوقعة (السنة الأولى)

الربع	هدف العملاء	الإيراد الشهري	إيراد الربع
Q1	10 عيادات	3,000 جنيه	9,000 جنيه
Q2	30 عيادة	15,000 جنيه	45,000 جنيه
Q3	75 عيادة	38,000 جنيه	114,000 جنيه
Q4	150 عيادة	80,000 جنيه	240,000 جنيه

 
11. Non-Functional Requirements

المتطلب	الهدف	كيفية التحقيق
Performance	صفحة تُحمَّل < 2 ثانية	Redis Cache + Dapper Optimized Queries
Availability	99.9% Uptime	Load Balancer + Health Checks + Auto-restart
Scalability	1000+ مستخدم متزامن	Horizontal Scaling + Docker + Kubernetes
Security	Zero Data Breach	Encryption + Audit Logs + Pen Testing
Usability	تعلم في < 30 دقيقة	UX بسيط + Onboarding Tutorial + Arabic UI
Data Backup	Zero Data Loss	Daily Automated Backup + Point-in-time Recovery
Compliance	معايير طبية	Audit Trail كامل + GDPR + Data Retention Policy

12. Project Folder Structure

Backend — Clean Architecture	ClinicFlow.Domain/ (Entities, Enums, Interfaces) | ClinicFlow.Application/ (UseCases, DTOs, Validators, Commands) | ClinicFlow.Infrastructure/ (Repositories, Dapper, Services) | ClinicFlow.API/ (Controllers, Middleware, Filters)

Frontend — Angular 18	src/app/core/ (Auth, Guards, Interceptors) | src/app/shared/ (Components, Directives, Pipes) | src/app/features/ (patients/, appointments/, visits/, billing/, reports/) | src/app/layout/ (Sidebar, Header, Footer)

Database Migrations	migrations/V001__create_tenants.sql | migrations/V002__create_patients.sql | migrations/V003__create_appointments.sql | migrations/V004__create_visits.sql | migrations/V005__seed_data.sql

13. Additional Tech Recommendations

الغرض	الأداة المقترحة	السبب
API Documentation	Scalar (بديل Swagger)	أجمل وأحدث من Swagger UI
Logging	Serilog + Seq	Structured Logging قوي مع Dashboard
Monitoring	Prometheus + Grafana	مراقبة الأداء وإشعارات تلقائية
Background Jobs	Hangfire	جدولة SMS والتذكيرات والتقارير
PDF Generation	QuestPDF	مكتبة .NET ممتازة للـ PDF
Email	MailKit + Brevo	قوي ومجاني للبداية
Health Checks	AspNetCore.HealthChecks	مراقبة DB/Redis/APIs
Testing	xUnit + FluentAssertions + Testcontainers	Unit + Integration Tests
CI/CD	GitHub Actions	Deploy تلقائي عند كل Push
Containerization	Docker + Docker Compose	بيئة موحدة Dev/Prod


ClinicFlow SaaS — Technical Documentation v1.0.0
Confidential — All Rights Reserved © 2026
