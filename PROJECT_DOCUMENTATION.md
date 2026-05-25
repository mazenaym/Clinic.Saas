# ClinicFlow SaaS - Business & Technical Documentation

## 1. الملخص التنفيذي

ClinicFlow SaaS هو نظام لإدارة العيادات الطبية بنموذج **Software as a Service**، يهدف إلى تحويل تشغيل العيادات من ملفات ورقية وجداول منفصلة إلى منصة رقمية موحدة لإدارة المرضى، المواعيد، الزيارات الطبية، الروشتات، المدفوعات، المستخدمين، والاشتراكات.

المشروع مبني كمنظومة Full-stack:

| الجزء | التقنية |
|---|---|
| Backend | ASP.NET Core Web API على .NET 10 |
| Architecture | Clean Architecture مقسمة إلى API, Service, Domain, Infrastructure |
| Database | SQL Server |
| Data Access | Dapper |
| Validation | FluentValidation |
| Mapping | AutoMapper |
| Authentication | JWT Access Token + Refresh Token |
| Frontend | Angular 21 Standalone Components |
| API Communication | RESTful APIs عبر JSON |

القيمة الأساسية للمشروع هي تقليل الوقت التشغيلي داخل العيادة، تحسين دقة السجلات الطبية، تقليل تضارب المواعيد، وتمكين صاحب العيادة أو مدير المنصة من متابعة الأداء المالي والتشغيلي بشكل مركزي.

---

## 2. Business Perspective

### 2.1 الهدف من المشروع والقيمة التجارية

يخدم ClinicFlow SaaS العيادات التي تحتاج إلى نظام موحد لإدارة العمليات اليومية دون بناء نظام داخلي مخصص لكل عيادة. بدلاً من استخدام الورق، Excel، أو أدوات غير مترابطة، يوفر النظام تجربة مركزية تغطي دورة العمل كاملة من تسجيل العيادة وحتى إدارة المريض والفوترة.

أهم القيم التجارية:

- **تحسين كفاءة التشغيل:** تقليل الوقت المستهلك في البحث عن ملفات المرضى وتنظيم المواعيد.
- **تقليل الأخطاء:** حفظ التاريخ المرضي، الحساسية، الزيارات، والروشتات بطريقة منظمة.
- **زيادة وضوح الإيرادات:** تقارير يومية وشهرية للمدفوعات والإيرادات والديون.
- **دعم النمو:** نموذج SaaS يسمح بإضافة عيادات متعددة وإدارة اشتراكاتها.
- **تحسين تجربة المريض:** مواعيد منظمة، سجل طبي واضح، وروشتة إلكترونية قابلة للرجوع إليها.

### 2.2 المشاكل التي يحلها المشروع

| المشكلة | الحل داخل ClinicFlow |
|---|---|
| فقدان أو تشتت بيانات المرضى | ملف مريض رقمي يحتوي البيانات الأساسية، التاريخ المرضي، الحساسية، التأمين، والزيارات |
| تضارب المواعيد | إدارة مواعيد يومية/أسبوعية/شهرية مع Availability وتحديث حالة الموعد |
| صعوبة متابعة الزيارات الطبية | تسجيل الزيارة، الشكوى، العلامات الحيوية، التشخيص، والمتابعة |
| الروشتات الورقية غير المنظمة | إنشاء Prescription مرتب يحتوي الأدوية والجرعات والتعليمات |
| ضعف الرقابة المالية | تسجيل المدفوعات، متابعة الديون، تقارير الإيرادات اليومية والشهرية |
| صعوبة إدارة المستخدمين | أدوار وصلاحيات: SuperAdmin, Admin, Doctor, Reception |
| تعدد العيادات على نفس المنصة | Multi-tenancy عبر TenantId وSubdomain لكل عيادة |

### 2.3 الجمهور المستهدف

| الجمهور | الاحتياج الأساسي |
|---|---|
| أصحاب العيادات | متابعة الأداء، الاشتراكات، المستخدمين، والإيرادات |
| الأطباء | الوصول إلى ملف المريض، تسجيل الزيارة، التشخيص، والروشتة |
| موظفو الاستقبال | إدارة المرضى والمواعيد وتسجيل المدفوعات |
| مدير المنصة Super Admin | إدارة العيادات، الاشتراكات، حالة العيادة، ولوحة التحكم العامة |
| المرضى بشكل غير مباشر | تجربة حجز ومتابعة أكثر تنظيماً |

### 2.4 الأدوار والصلاحيات

| الدور | الصلاحيات العامة |
|---|---|
| SuperAdmin | إدارة المنصة، العيادات، الاشتراكات، إحصائيات المنصة |
| Admin | إدارة عيادة محددة، المستخدمين، الإعدادات، المرضى، التقارير |
| Doctor | إدارة الزيارات الطبية، الروشتات، متابعة المرضى والمواعيد |
| Reception | إدارة المرضى، المواعيد، والمدفوعات |

### 2.5 Workflows الرئيسية

#### Workflow 1: تسجيل عيادة جديدة

1. المستخدم يطلب تسجيل عيادة من واجهة Onboarding.
2. النظام يتحقق من توفر الـ subdomain.
3. يتم إنشاء Tenant جديد وAdmin أولي للعيادة.
4. يرجع النظام جلسة تسجيل دخول تحتوي Access Token وRefresh Token.
5. يبدأ المستخدم في إعداد العيادة والمستخدمين.

#### Workflow 2: تسجيل الدخول

1. المستخدم يدخل البريد الإلكتروني وكلمة المرور وربما subdomain.
2. Backend يتحقق من بيانات المستخدم وحالة العيادة.
3. النظام يصدر JWT يحتوي بيانات المستخدم والـ Tenant.
4. Frontend يحفظ الجلسة في `localStorage`.
5. كل طلب لاحق يرسل `Authorization: Bearer <token>`.

#### Workflow 3: إدارة المرضى

1. Reception أو Doctor أو Admin يضيف مريضاً جديداً.
2. يتم حفظ بيانات المريض تحت Tenant العيادة.
3. يمكن البحث بالاسم أو الهاتف أو كود المريض.
4. يمكن عرض تفاصيل المريض، تحديثها، أو حذفها Soft Delete.
5. يمكن عرض Timeline المريض من عمليات و زيارات ومدفوعات.

#### Workflow 4: إدارة المواعيد

1. المستخدم يختار المريض والطبيب والتاريخ والوقت.
2. النظام يتحقق من Availability.
3. يتم إنشاء الموعد بحالة Scheduled أو ما يعادلها.
4. يمكن تغيير الحالة إلى Confirmed, Completed, Cancelled, NoShow.
5. يمكن عرض المواعيد اليومية، الأسبوعية، والشهرية.

#### Workflow 5: الزيارة الطبية والروشتة

1. الطبيب يفتح ملف المريض أو موعد اليوم.
2. يتم إنشاء Visit تحتوي الشكوى، العلامات الحيوية، الملاحظات، التشخيص، والمتابعة.
3. يمكن إنشاء Prescription مرتبطة بالزيارة.
4. يتم حفظ أدوية الروشتة، الجرعات، المدة، وطريقة الاستخدام.
5. يمكن لاحقاً توليد PDF أو إرسال الروشتة عبر WhatsApp حسب نقاط الـ API المجهزة.

#### Workflow 6: المدفوعات والتقارير

1. Reception أو Admin يسجل Payment بعد الزيارة أو الخدمة.
2. يتم حفظ بيانات الفاتورة، الخصم، المدفوع، والمتبقي.
3. يمكن مراجعة مدفوعات المريض أو الديون.
4. Admin يمكنه عرض تقرير الإيراد اليومي والشهري.

#### Workflow 7: إدارة المنصة

1. SuperAdmin يدخل إلى لوحة Admin.
2. يستعرض إجمالي العيادات، المستخدمين، المرضى، الاشتراكات، والإيرادات.
3. يمكنه إنشاء عيادة، تحديث بياناتها، تفعيل/تعطيل العيادة، وإضافة اشتراك.

---

## 3. Technical Perspective

## 3.1 Backend Architecture

### 3.1.1 نظرة عامة

الـ Backend مبني باستخدام **ASP.NET Core Web API** على **.NET 10**، ومقسم إلى طبقات قريبة من Clean Architecture:

| الطبقة | المشروع | المسؤولية |
|---|---|---|
| Presentation/API | `Clinic.Saas` | Controllers, Middleware, Swagger, Auth/CORS setup |
| Application/Service | `Clinic.Saas.Service` | Use Cases, Commands, Queries, DTOs, Validators, Authorization services |
| Domain | `Clinic.Saas.Domain` | Entities, Enums, Repository Interfaces |
| Infrastructure | `Clinic.Saas.Infrastructure` | Dapper, SQL Connection, Repository Implementations, JWT, Password Hashing |

هذا الفصل يجعل قواعد العمل منفصلة عن تفاصيل قاعدة البيانات وHTTP، ويسهل اختبار الـ Use Cases وتطوير النظام تدريجياً.

### 3.1.2 التقنيات المستخدمة في Backend

| التقنية | الاستخدام |
|---|---|
| ASP.NET Core Controllers | بناء REST APIs |
| JWT Bearer Authentication | تأمين الطلبات باستخدام Access Token |
| Refresh Token | تجديد الجلسة دون إعادة تسجيل الدخول |
| Dapper | تنفيذ SQL Queries بكفاءة عالية |
| SQL Server | قاعدة البيانات الأساسية |
| FluentValidation | Validation مستقل للـ DTOs والـ Commands |
| AutoMapper | تحويل Entities إلى DTOs والعكس |
| Swagger / Swashbuckle | توثيق وتجربة الـ APIs |
| ASP.NET PasswordHasher | Hashing لكلمات المرور |
| CORS Policy | السماح لواجهة Angular بالاتصال بالـ API |

### 3.1.3 تنظيم الـ Entities

الـ Entities موجودة في `Clinic.Saas.Domain/Entities` وتمثل نموذج الدومين الأساسي:

| Entity | الوصف |
|---|---|
| `Tenant` | العيادة أو العميل داخل نموذج SaaS |
| `Subscription` | اشتراك العيادة وخطتها وحالتها |
| `User` | مستخدم داخل عيادة، مع دور وصلاحيات |
| `Patient` | ملف المريض |
| `Appointment` | موعد بين مريض وطبيب |
| `Visit` | زيارة طبية وتفاصيل الكشف |
| `Prescription` / `PrescriptionItem` | الروشتة والأدوية |
| `Payment` / `PaymentItem` | الفاتورة والمدفوعات |
| `ClinicSetting` | إعدادات العيادة |
| `AuditLog` | سجل العمليات |

مثال مبسط من نموذج المريض:

```csharp
public class Patient
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string PatientCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? MedicalHistory { get; set; }
    public string? DrugAllergies { get; set; }
    public bool IsDeleted { get; set; }
}
```

### 3.1.4 Repositories و Data Access

طبقة Infrastructure تحتوي تنفيذ الـ Repositories باستخدام Dapper. الاتصال بقاعدة البيانات يتم عبر `DapperContext`:

```csharp
public IDbConnection CreateConnection()
    => new SqlConnection(_connectionString);
```

الـ Repository Interfaces موجودة في Domain، مثل:

- `IPatientRepository`
- `IAppointmentRepository`
- `IVisitRepository`
- `IPrescriptionRepository`
- `IPaymentRepository`
- `IUserRepository`
- `ITenantRepository`

أما التنفيذ الفعلي فيوجد في Infrastructure:

- `PatientRepository`
- `AppointmentRepository`
- `VisitRepository`
- `PrescriptionRepository`
- `PaymentRepository`
- `UserRepository`
- `TenantRepository`
- `PlatformAdminRepository`

هذا يسمح بأن تبقى طبقة الـ Service معتمدة على abstractions، وليس على SQL أو Dapper مباشرة.

### 3.1.5 Services و Use Cases

طبقة Service منظمة حول Commands وQueries:

| النوع | أمثلة |
|---|---|
| Commands | `CreatePatientCommand`, `CreateAppointmentCommand`, `CreateVisitCommand`, `CreatePaymentCommand` |
| Queries | `GetAllPatientsQuery`, `GetAppointmentsByDateQuery`, `GetDailyRevenueReportQuery` |
| Validators | `CreatePatientValidator`, `LoginValidator`, `CreateAppointmentValidator` |
| Services | `ClinicAuthorizationService` |

مثال تدفق إنشاء مريض:

1. `PatientsController` يستقبل الطلب.
2. `CreatePatientValidator` يتحقق من البيانات.
3. `CreatePatientCommand.Handler` ينفذ منطق العمل.
4. `IPatientRepository` يحفظ البيانات.
5. يرجع `BaseResponse<T>` للواجهة.

### 3.1.6 API Endpoints الهامة

#### Authentication & Onboarding

| Method | Endpoint | الوصف | الحماية |
|---|---|---|---|
| POST | `/api/Auth/login` | تسجيل الدخول | Public |
| POST | `/api/Auth/refresh` | تجديد Access Token | Public عبر Refresh Token |
| POST | `/api/Auth/logout` | تسجيل الخروج | Authenticated |
| GET | `/api/onboarding/check-subdomain` | التحقق من توفر subdomain | Public |
| POST | `/api/onboarding/register-clinic` | تسجيل عيادة جديدة | Public |

#### Patients

| Method | Endpoint | الوصف | الأدوار |
|---|---|---|---|
| GET | `/api/Patients` | عرض المرضى | Admin, Doctor, Reception |
| GET | `/api/Patients/{id}` | عرض مريض محدد | Admin, Doctor, Reception |
| GET | `/api/Patients/search` | بحث عن مريض | Admin, Doctor, Reception |
| POST | `/api/Patients` | إنشاء مريض | Admin, Doctor, Reception |
| PUT | `/api/Patients/{id}` | تحديث بيانات مريض | Admin, Doctor, Reception |
| DELETE | `/api/Patients/{id}` | حذف/تعطيل مريض | Admin, Reception |

#### Appointments

| Method | Endpoint | الوصف | الأدوار |
|---|---|---|---|
| POST | `/api/Appointments` | إنشاء موعد | Admin, Doctor, Reception |
| GET | `/api/Appointments/daily` | مواعيد يوم محدد | Admin, Doctor, Reception |
| GET | `/api/Appointments/availability` | الأوقات المتاحة | Admin, Doctor, Reception |
| PUT | `/api/Appointments/{id}/status` | تحديث حالة الموعد | Admin, Doctor, Reception |

#### Visits & Prescriptions

| Method | Endpoint | الوصف | الأدوار |
|---|---|---|---|
| POST | `/api/Visits` | إنشاء زيارة | Admin, Doctor |
| GET | `/api/Visits/{id}` | عرض زيارة | Admin, Doctor |
| POST | `/api/Prescriptions` | إنشاء روشتة | Admin, Doctor |
| GET | `/api/Prescriptions/{id}` | عرض روشتة | Admin, Doctor |

#### Billing

| Method | Endpoint | الوصف | الأدوار |
|---|---|---|---|
| POST | `/api/Billing/payments` | إنشاء دفعة/فاتورة | Admin, Reception |
| GET | `/api/Billing/reports/daily-revenue` | تقرير الإيراد اليومي | Admin |

#### Users & Admin

| Method | Endpoint | الوصف | الأدوار |
|---|---|---|---|
| GET | `/api/Users/me` | المستخدم الحالي | Authenticated |
| GET | `/api/Users` | مستخدمو العيادة | Admin |
| POST | `/api/Users` | إنشاء مستخدم | Admin |
| GET | `/api/admin/dashboard` | لوحة تحكم المنصة | SuperAdmin |
| GET | `/api/admin/clinics` | قائمة العيادات | SuperAdmin |
| POST | `/api/admin/clinics` | إنشاء عيادة | SuperAdmin |
| PATCH | `/api/admin/clinics/{id}/status` | تفعيل/تعطيل عيادة | SuperAdmin |

#### Operations API

يوجد Controller موسع تحت `/api/operations` لتجميع وظائف تشغيلية إضافية مثل:

- إعدادات العيادة.
- Preferences للمستخدم الحالي.
- Timeline المريض.
- رفع مستندات المرضى.
- تقارير المواعيد.
- إدارة الحجوزات الإلكترونية.
- Finalize Visit.
- Clinical Templates.
- توليد PDF للروشتة أو الإيصال.
- فحص تداخلات الأدوية.
- تقارير الديون والإيرادات الشهرية.
- Activity Log.

### 3.1.7 تأمين الـ Backend

يستخدم النظام JWT Bearer Authentication. عند تسجيل الدخول، يتم إصدار:

- `accessToken`: قصير العمر، افتراضياً 15 دقيقة.
- `refreshToken`: طويل نسبياً، افتراضياً 7 أيام.

الـ JWT يحتوي Claims مهمة:

```csharp
new(ClaimTypes.Role, user.Role.ToString()),
new("tenant_id", tenant.Id.ToString()),
new("tenant_subdomain", tenant.Subdomain),
new("tenant_name", tenant.Name)
```

آليات الأمان:

- حماية الـ APIs باستخدام `[Authorize]`.
- تقييد بعض الـ APIs باستخدام `[Authorize(Roles = "...")]`.
- فصل بيانات العيادات عبر `TenantId`.
- `TenantAccessMiddleware` يمنع الوصول إذا كانت العيادة غير فعالة أو اشتراكها منتهي.
- Hashing لكلمات المرور باستخدام `PasswordHasher`.
- CORS مضبوط للسماح ببيئات localhost أو origins محددة من الإعدادات.
- Swagger يدعم Bearer Token لتجربة الـ APIs المحمية.

### 3.1.8 Multi-Tenancy

النظام مصمم لخدمة أكثر من عيادة على نفس المنصة. كل كيان تشغيلي تقريباً يحتوي `TenantId`، مثل:

- Users
- Patients
- Appointments
- Visits
- Prescriptions
- Payments

يتم تحديد العيادة بطريقتين:

1. من الـ JWT claim: `tenant_id`.
2. من الـ subdomain عبر `TenantResolutionMiddleware` عندما يكون الطلب على نطاق مثل:

```text
clinic-a.example.com
```

---

## 3.2 Frontend Architecture

### 3.2.1 نظرة عامة

الواجهة مبنية باستخدام **Angular 21** مع Standalone Components وLazy Loading. التطبيق يعتمد على routing واضح، Guards للصلاحيات، وService مركزي لاستهلاك الـ APIs.

### 3.2.2 التقنيات المستخدمة في Frontend

| التقنية | الاستخدام |
|---|---|
| Angular 21 | بناء الواجهة |
| Standalone Components | تقليل الاعتماد على NgModules التقليدية |
| Angular Router | التنقل بين الصفحات |
| Route Guards | حماية الصفحات حسب حالة الدخول والدور |
| HttpClient | استهلاك REST APIs |
| Http Interceptor | إضافة JWT وتجديد الجلسة تلقائياً |
| Signals / Computed | State محلي للجلسة والمستخدم |
| RxJS | التعامل مع HTTP streams |
| SCSS | تنسيق الواجهة |

### 3.2.3 تنظيم الواجهة

```text
Clinic.Saas.Frontend/src/app
├── core
│   ├── api.service.ts
│   ├── auth.service.ts
│   ├── auth.guard.ts
│   ├── auth.interceptor.ts
│   └── models.ts
├── features
│   ├── auth
│   ├── dashboard
│   ├── patients
│   ├── appointments
│   ├── visits
│   ├── prescriptions
│   ├── billing
│   ├── operations
│   ├── users
│   └── admin
└── shared
    └── shell
```

### 3.2.4 Routing و Lazy Loading

الصفحات يتم تحميلها lazy عند الحاجة:

```typescript
{
  path: 'patients',
  loadComponent: () =>
    import('./features/patients/patients.component')
      .then((m) => m.PatientsComponent)
}
```

المسارات الرئيسية:

| Route | الصفحة | الحماية |
|---|---|---|
| `/auth` | تسجيل الدخول / التسجيل | Guest only |
| `/dashboard` | لوحة التحكم | Authenticated |
| `/patients` | المرضى | Authenticated |
| `/appointments` | المواعيد | Authenticated |
| `/visits` | الزيارات | Admin, Doctor |
| `/prescriptions` | الروشتات | Admin, Doctor |
| `/billing` | المدفوعات | Admin, Reception |
| `/operations` | عمليات إضافية | Authenticated |
| `/users` | المستخدمون | Admin |
| `/admin` | إدارة المنصة | SuperAdmin |

### 3.2.5 استهلاك الـ APIs

كل استدعاءات الـ Backend تقريباً متمركزة في `ApiService`، مما يسهل تغيير base URL أو توحيد شكل التعامل مع `ApiResponse<T>`.

```typescript
private readonly baseUrl = '/api';

private get<T>(path: string, params?: Record<string, string>) {
  return this.http
    .get<ApiResponse<T>>(`${this.baseUrl}${path}`, { params: this.params(params) })
    .pipe(map((r) => r.data));
}
```

### 3.2.6 State Management

التطبيق يستخدم State Management خفيف مبني على Angular Signals داخل `AuthService`:

```typescript
readonly session = signal<AuthSession | null>(this.readSession());
readonly user = computed(() => this.session()?.user ?? null);
readonly tenant = computed(() => this.session()?.tenant ?? null);
readonly isAuthenticated = computed(() => Boolean(this.session()?.accessToken));
```

الجلسة تحفظ في:

```text
localStorage: clinicflow.session
```

هذا مناسب للـ MVP لأنه يقلل التعقيد، ويمكن لاحقاً الانتقال إلى Store أكثر توسعاً إذا زادت تعقيدات الحالة.

### 3.2.7 Guards و Authorization في الواجهة

يوجد ثلاث طبقات حماية في routing:

- `authGuard`: يمنع غير المسجلين من دخول التطبيق.
- `guestGuard`: يمنع المستخدم المسجل من صفحة auth.
- `roleGuard`: يسمح بالمسار حسب الدور.

مثال:

```typescript
{
  path: 'billing',
  canActivate: [roleGuard(['Admin', 'Reception'])],
  loadComponent: () => import('./features/billing/billing.component')
    .then((m) => m.BillingComponent)
}
```

---

## 4. Communication Between Frontend & Backend

### 4.1 نمط الاتصال

الاتصال بين Angular وASP.NET Core يتم عبر **RESTful APIs** بصيغة JSON.

في بيئة التطوير، Angular يستخدم proxy:

```json
{
  "/api": {
    "target": "http://localhost:5232",
    "secure": false,
    "changeOrigin": true
  }
}
```

هذا يعني أن الواجهة تستدعي:

```text
/api/Patients
```

ثم يقوم Angular dev server بتحويلها إلى:

```text
http://localhost:5232/api/Patients
```

### 4.2 شكل الاستجابة القياسي

الواجهة تتوقع استجابة موحدة:

```typescript
export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors?: string[];
  statusCode: number;
}
```

هذا يسهل التعامل مع النجاح والفشل على مستوى موحد.

### 4.3 Authentication Flow

1. Frontend يرسل بيانات الدخول إلى `/api/Auth/login`.
2. Backend يرجع `accessToken`, `refreshToken`, `expiresAt`, وبيانات المستخدم والعيادة.
3. Frontend يخزن الجلسة محلياً.
4. `authInterceptor` يضيف الـ token تلقائياً لكل طلب.
5. إذا اقترب انتهاء الـ token أو رجع 401، يتم استدعاء `/api/Auth/refresh`.
6. إذا فشل التجديد، يتم مسح الجلسة وإرجاع المستخدم إلى `/auth`.

مثال Header:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

### 4.4 بروتوكولات الأمان

| الجانب | التطبيق الحالي |
|---|---|
| Authentication | JWT Bearer |
| Authorization | Role-based Authorization |
| Session Renewal | Refresh Token |
| Password Security | ASP.NET PasswordHasher |
| Tenant Isolation | `tenant_id` claim و`TenantId` في الجداول |
| CORS | Policy باسم `ClinicSaasCors` |
| HTTPS | مفعل خارج Development عبر `UseHttpsRedirection` |
| Subscription Enforcement | `TenantAccessMiddleware` يرجع 402 عند انتهاء الاشتراك |

---

## 5. Infrastructure & Deployment

### 5.1 متطلبات بيئة التشغيل

| المتطلب | الإصدار/الوصف |
|---|---|
| .NET SDK | .NET 10 |
| Node.js / npm | مناسب لتشغيل Angular 21 |
| Angular CLI | مستخدم عبر `ng` |
| SQL Server | قاعدة بيانات `ClinicFlow` |
| Browser | لتشغيل واجهة Angular |

### 5.2 إعداد قاعدة البيانات

ملفات قاعدة البيانات موجودة داخل مجلد:

```text
database/
├── schema.sql
├── feature-pack-migration.sql
└── admin-dashboard-migration.sql
```

ملف الاتصال الحالي في `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=ClinicFlow;Trusted_Connection=True;..."
  }
}
```

في بيئات الإنتاج يجب نقل القيم الحساسة إلى:

- Environment Variables
- Azure Key Vault أو Secret Manager مشابه
- CI/CD secrets

ولا يفضل ترك `Jwt:Key` أو Connection String الفعلية داخل repository عام.

### 5.3 تشغيل Backend محلياً

من جذر المشروع:

```bash
dotnet restore
dotnet build
dotnet run --project Clinic.Saas/Clinic.Saas.api.csproj
```

الـ API يعمل محلياً حسب `launchSettings.json` على:

```text
http://localhost:5232
https://localhost:7299
```

Swagger متاح عادة على:

```text
http://localhost:5232/swagger
```

### 5.4 تشغيل Frontend محلياً

```bash
cd Clinic.Saas.Frontend
npm install
npm start
```

`npm start` يشغل:

```bash
ng serve --proxy-config proxy.conf.json
```

### 5.5 Build للإنتاج

Backend:

```bash
dotnet publish Clinic.Saas/Clinic.Saas.api.csproj -c Release -o ./publish/api
```

Frontend:

```bash
cd Clinic.Saas.Frontend
npm run build
```

### 5.6 اعتبارات النشر المقترحة

لبيئة Production يوصى بالآتي:

- نشر Backend خلف Reverse Proxy مثل Nginx أو IIS.
- تفعيل HTTPS وشهادة TLS.
- ضبط `Cors:AllowedOrigins` على دومينات الواجهة الفعلية فقط.
- استخدام SQL Server production instance مع backups دورية.
- تدوير `Jwt:Key` وحفظه خارج الكود.
- مراقبة logs، أخطاء 500، محاولات الدخول الفاشلة، واستهلاك قاعدة البيانات.
- إضافة Health Checks للـ API وقاعدة البيانات.
- تجهيز CI/CD pipeline للبناء، الاختبارات، والنشر.

---

## 6. ملاحظات تصميمية ومعمارية

### نقاط قوة حالية

- فصل جيد بين Domain وService وInfrastructure وAPI.
- استخدام Dapper مناسب للأداء والتحكم المباشر في SQL.
- استخدام JWT وRefresh Token مناسب لتطبيق SaaS.
- دعم واضح للأدوار والصلاحيات.
- وجود Multi-tenancy عبر TenantId وSubdomain.
- Frontend منظم إلى Core وFeatures وShared.
- API Service مركزي يقلل التكرار في استدعاءات HTTP.

### نقاط يمكن تحسينها لاحقاً

| المجال | التحسين المقترح |
|---|---|
| Secrets | نقل JWT Key وConnection String إلى Secret Store |
| Testing | إضافة Unit Tests للـ Use Cases وIntegration Tests للـ Controllers |
| Observability | إضافة structured logging وrequest correlation id |
| Database Migrations | اعتماد آلية migrations منظمة بدلاً من ملفات SQL يدوية فقط |
| Authorization | توحيد policies وربطها بمتطلبات business أكثر تفصيلاً |
| API Documentation | إضافة XML comments أو OpenAPI descriptions |
| Frontend State | دراسة Store مركزي عند زيادة تعقيد البيانات |
| Audit | توسيع AuditLog ليغطي العمليات الحساسة مثل تعديل المدفوعات والصلاحيات |

---

## 7. الخلاصة

ClinicFlow SaaS مشروع عيادات متكامل يجمع بين قيمة بيزنس واضحة وبنية تقنية قابلة للتوسع. من منظور المنتج، هو يحل مشاكل تشغيلية يومية للعيادات: المرضى، المواعيد، الزيارات، الروشتات، المدفوعات، والتقارير. ومن منظور التقنية، يعتمد على Stack حديث نسبياً: .NET 10، Angular 21، SQL Server، Dapper، JWT، وClean Architecture.

البنية الحالية مناسبة جداً لمرحلة MVP وما بعدها، بشرط الاستمرار في تقوية الاختبارات، إدارة الأسرار، التوثيق الآلي للـ APIs، ومراقبة التشغيل عند الانتقال إلى Production.
