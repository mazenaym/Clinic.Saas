# Backend Stabilization Plan

هذا الملف هدفه يقلل التوهان. اعتبره خريطة تمشي عليها مع الباك اند خطوة خطوة.

## 1. خريطة المشروع ببساطة

المشروع مقسوم لاربع طبقات رئيسية:

| الطبقة | معناها | المفروض تعمل ايه | امثلة ملفات |
|---|---|---|---|
| API / Controllers | باب دخول الطلبات من الفرونت | يستقبل HTTP فقط ويرجع response | `Clinic.Saas/Controllers` |
| Service / UseCases | منطق البيزنس | يتحقق من البيانات ويقرر السيناريو | `Clinic.Saas.Service/UseCases` |
| Infrastructure | التعامل مع SQL والخدمات الخارجية | Dapper queries, auth helpers, repositories | `Clinic.Saas.Infrastructure` |
| Domain | تعريف الكيانات الاساسية | Patient, Appointment, User, Payment | `Clinic.Saas.Domain` |

المسار الطبيعي لاي feature:

```text
Frontend
  -> Controller
  -> UseCase / Handler
  -> Repository
  -> SQL Server
```

اي SQL مباشر داخل Controller يعتبر منطقة تحتاج تنظيف لاحقا، خصوصا `OperationsController`.

## 2. انت واقف فين حاليا

الحالة الحالية:

```text
Prototype غني بالfeatures
لكن محتاج Stabilization قبل التوسع
```

يعني المشروع فيه حاجات كتير، لكنه لسه محتاج:

- تثبيت tenant isolation.
- اصلاح bugs حرجة.
- نقل SQL من controllers الى use cases/repositories.
- توحيد شكل الAPI والاخطاء.
- اضافة اختبارات بسيطة تحميك من كسر الحاجات المهمة.

## 3. ترتيب الاولويات

امشي بالترتيب ده ولا تقفز:

| الاولوية | الهدف | ليه مهمة |
|---|---|---|
| P0 | اصلاح bugs وsecurity blockers | عشان المشروع يبقى قابل للتجربة بدون كسر خطير |
| P1 | تثبيت tenant isolation | عشان عيادة ما تشوفش بيانات عيادة تانية |
| P2 | تنظيف المعمارية | عشان تفهم المشروع وتقدر تكمله |
| P3 | بناء MVP flow | المرضى والمواعيد والدفع الاساسي |
| P4 | تحسينات production | تقارير، audit اعمق، RLS، temporal tables، performance |

## 4. P0 - لازم يتعمل قبل اي Features جديدة

| Task | ايه اللي مش شغال / خطر | هيصلح ايه | هيتحل باستخدام ايه | الملفات المتوقعة |
|---|---|---|---|---|
| Fix change password | تغيير كلمة المرور بيقارن الباسورد بالعكس | المستخدم يقدر يغير الباسورد فعلا | تعديل ترتيب parameters في `VerifyPassword` | `OperationsController.cs` |
| Fix payment creation | انشاء payment ممكن يفشل لان `RemainingAmount` و `TotalPrice` مش بيتحفظوا | الفاتورة تتسجل صح في SQL | تعديل Dapper INSERT | `PaymentRepository.cs` |
| Remove JWT secret from config | سر JWT مكتوب في `appsettings.json` | تقليل خطر تسريب السر | user-secrets او environment variable | `appsettings.json`, local secrets |
| Stop client TenantId | بعض DTOs فيها `TenantId` جاي من الفرونت | الباك اند فقط يحدد العيادة | حذف/تجاهل TenantId من request DTOs | `CreatePatientDto.cs`, controllers |
| Secure patient uploads | upload بيرجع path داخلي ومفيش تحقق كافي | تقليل خطر ملفات ضارة او path leakage | file validation + storage abstraction | `OperationsController.cs` |

## 5. P1 - تثبيت Tenant Isolation

| Task | ايه المشكلة | هيصلح ايه | هيتحل باستخدام ايه | الملفات المتوقعة |
|---|---|---|---|---|
| Tenant connection factory | `DapperContext` بيرجع connection عادي بدون session tenant | كل SQL connection تعرف TenantId/UserId | `sp_set_session_context` | `DapperContext.cs` او class جديد |
| Tenant-safe repositories | في generic methods من غير tenant | منع cross-tenant access بالغلط | حذف/تقليل `GetById(Guid id)` و `GetAll()` | repository interfaces/classes |
| Tenant-aware foreign keys | FKs بتربط على `Id` فقط | SQL نفسه يمنع cross-tenant references | composite FK `(TenantId, Id)` | `database/schema.sql` |
| Add RLS later | مفيش Row Level Security | دفاع اضافي على مستوى SQL Server | Security predicate + policy | migration SQL |

## 6. P2 - تنظيف المعمارية

| Task | ايه المشكلة | هيصلح ايه | هيتحل باستخدام ايه | الملفات المتوقعة |
|---|---|---|---|---|
| Split OperationsController | controller واحد ماسك حاجات كتير جدا | المشروع يبقى مفهوم | نقل كل feature الى controller/usecase خاص | `OperationsController.cs`, new use cases |
| Move SQL out of API | controllers بتكلم DB مباشرة | Clean Architecture | repositories/query services | `Service`, `Infrastructure` |
| Standard API routes | routes مختلطة | API اسهل للفهم والتوثيق | `/api/v1/...` convention | controllers |
| Standard errors | `BaseResponse` فقط ومفيش ProblemDetails | اخطاء واضحة للفرونت | `AddProblemDetails`, exception middleware | `Program.cs`, middleware |
| Permission policies | الاعتماد على roles فقط | صلاحيات ادق | policies مثل `Patients.View`, `Billing.Manage` | `Program.cs`, auth services |

## 7. P3 - MVP Flow

الهدف هنا اول نسخة عملية للعيادة، مش كل المنتج.

| المرحلة | المطلوب | النتيجة |
|---|---|---|
| Patients | create, update, search, details | الريسبشن يعرف يسجل ويلاقي المريض |
| Appointments | book, reschedule, cancel, check-in, daily view | الريسبشن يعرف يدير اليوم |
| Visits | create visit, clinical notes, diagnosis | الدكتور يعرف يسجل زيارة |
| Billing | create invoice/payment, receipt, daily revenue | العيادة تعرف تسجل فلوس |

## 8. طريقة التنفيذ خطوة خطوة

اتبع القاعدة دي:

```text
Task واحدة
ملفات قليلة
Build
اختبار سريع
Commit
بعدها task جديدة
```

لا تطلب من AI "كمل المشروع".

استخدم prompt بالشكل ده:

```text
اشتغل على Task واحدة فقط:
Task: Fix payment creation.
المشكلة: Payment INSERT لا يحفظ RemainingAmount وPaymentItems لا تحفظ TotalPrice.
المطلوب: تعديل PaymentRepository فقط واضافة/تعديل test لو موجود.
الممنوع: لا تعمل refactor للbilling كله.
التحقق: شغل dotnet build وقولي النتيجة.
```

## 9. اول 15 Task بالترتيب

1. Fix change password bug.
2. Fix payment creation insert bug.
3. Move JWT secret out of `appsettings.json`.
4. Remove/ignore client-sent `TenantId` from patient DTOs.
5. Add quick tests for login and change password.
6. Add quick tests for create payment.
7. Review all repositories for methods without `TenantId`.
8. Disable or restrict unsafe generic repository methods.
9. Start splitting `OperationsController` by feature.
10. Move patient operations SQL into patient use cases/repositories.
11. Move appointment operations SQL into appointment use cases/repositories.
12. Add `/api/v1` route convention.
13. Add ProblemDetails middleware.
14. Add `RowVersion` to important mutable tables.
15. Add tenant-aware foreign keys migration plan.

## 10. ازاي تعرف انك ماشي صح

انت ماشي صح لما تقدر تجاوب بنعم على الاسئلة دي:

- هل المستخدم يقدر يعمل login/logout/refresh/change password؟
- هل كل request يعرف TenantId من الtoken مش من body؟
- هل مريض عيادة A مستحيل يظهر لعيادة B؟
- هل اقدر اضيف مريض واحجز له موعد؟
- هل اقدر امنع تعارض مواعيد نفس الدكتور؟
- هل اقدر اسجل payment بدون SQL error؟
- هل `dotnet build` شغال؟
- هل عندي tests بسيطة لاهم flows؟

لو الاجابات دي بقت نعم، عندك backend MVP ثابت تقدر تكمل عليه بثقة.
