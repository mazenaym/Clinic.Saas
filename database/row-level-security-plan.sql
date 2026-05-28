/*
Row-Level Security rollout plan

Do not run this directly on production without a staged rollout.

Important:
- RLS must only be enabled after every tenant-owned query path uses
  IDbConnectionFactory and calls sp_set_session_context for TenantId before
  any SELECT/INSERT/UPDATE/DELETE.
- Start with Patients only. Patients is broad enough to prove tenant filtering,
  but narrow enough to debug before protecting more operational tables.
- After local/dev validation, apply the same predicate to Appointments, Visits,
  Payments, and PatientDocuments.
- Keep explicit WHERE TenantId = @TenantId in application SQL even after RLS.
  The explicit predicate helps readability, query plans, and defense in depth.
*/

SET XACT_ABORT ON;
GO

------------------------------------------------------------------------------
-- Security schema
-- Risk closed: keeps tenant predicate functions and policies isolated from
-- application tables and avoids mixing security objects into dbo.
------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'Security')
BEGIN
    EXEC(N'CREATE SCHEMA Security AUTHORIZATION dbo;');
END;
GO

------------------------------------------------------------------------------
-- Tenant predicate
-- Risk closed: blocks rows whose TenantId does not match the TenantId stored
-- in the current SQL session context.
--
-- Required before enabling:
-- IDbConnectionFactory must execute something equivalent to:
--
-- EXEC sys.sp_set_session_context
--     @key = N'TenantId',
--     @value = @TenantId,
--     @read_only = 1;
--
-- The @read_only = 1 flag is important so downstream code cannot mutate the
-- TenantId inside the same logical connection.
------------------------------------------------------------------------------
CREATE OR ALTER FUNCTION Security.fn_tenantPredicate(@TenantId UNIQUEIDENTIFIER)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
    SELECT 1 AS fn_tenantPredicate_result
    WHERE @TenantId = TRY_CAST(SESSION_CONTEXT(N'TenantId') AS UNIQUEIDENTIFIER);
GO

------------------------------------------------------------------------------
-- Initial policy: Patients only
-- Risk closed: prevents accidental cross-tenant reads and writes on Patients
-- while keeping the first rollout small enough to test safely.
--
-- Start with Patients only. After testing, add:
-- - dbo.Appointments
-- - dbo.Visits
-- - dbo.Payments
-- - dbo.PatientDocuments
--
-- Note:
-- This block intentionally creates the policy only if missing. If you need to
-- change the policy later, use ALTER SECURITY POLICY after review.
------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantAccessPolicy' AND schema_id = SCHEMA_ID(N'Security'))
BEGIN
    CREATE SECURITY POLICY Security.TenantAccessPolicy
        ADD FILTER PREDICATE Security.fn_tenantPredicate(TenantId) ON dbo.Patients,
        ADD BLOCK PREDICATE Security.fn_tenantPredicate(TenantId) ON dbo.Patients AFTER INSERT,
        ADD BLOCK PREDICATE Security.fn_tenantPredicate(TenantId) ON dbo.Patients AFTER UPDATE
    WITH (STATE = OFF);
END;
GO

------------------------------------------------------------------------------
-- Enable policy only after application validation
-- Risk closed: keeps the plan non-destructive by default. Review and run this
-- manually only after IDbConnectionFactory session context is proven on local.
------------------------------------------------------------------------------
-- ALTER SECURITY POLICY Security.TenantAccessPolicy WITH (STATE = ON);
-- GO

------------------------------------------------------------------------------
-- Test SQL: SESSION_CONTEXT behavior
--
-- Replace the GUIDs below with real TenantId values from your local database.
-- These examples are comments on purpose. Run them manually in a local/dev DB.
------------------------------------------------------------------------------

-- 1) Pick two tenants:
-- DECLARE @TenantA UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';
-- DECLARE @TenantB UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000002';

-- 2) Set SQL session TenantId, matching what IDbConnectionFactory must do:
-- EXEC sys.sp_set_session_context @key = N'TenantId', @value = @TenantA, @read_only = 1;

-- 3) With policy ON, this should return only TenantA rows:
-- SELECT Id, TenantId, FullName
-- FROM dbo.Patients
-- ORDER BY CreatedAt DESC;

-- 4) With policy ON, this should return no rows even with an explicit filter:
-- SELECT Id, TenantId, FullName
-- FROM dbo.Patients
-- WHERE TenantId = @TenantB;

-- 5) With policy ON, this should fail or affect no valid rows when attempting
--    to insert/update Patient rows for a tenant different from SESSION_CONTEXT.
--    Use a transaction and roll it back during manual testing.
-- BEGIN TRANSACTION;
-- INSERT INTO dbo.Patients
-- (
--     Id, TenantId, PatientCode, FullName, PhoneNumber, Gender,
--     IsActive, IsDeleted, CreatedAt, UpdatedAt
-- )
-- VALUES
-- (
--     NEWID(), @TenantB, N'RLS-TEST', N'RLS Test', N'000',
--     1, 1, 0, SYSUTCDATETIME(), SYSUTCDATETIME()
-- );
-- ROLLBACK TRANSACTION;

------------------------------------------------------------------------------
-- Future rollout template after Patients passes local/dev testing
------------------------------------------------------------------------------
-- ALTER SECURITY POLICY Security.TenantAccessPolicy
--     ADD FILTER PREDICATE Security.fn_tenantPredicate(TenantId) ON dbo.Appointments,
--     ADD BLOCK PREDICATE Security.fn_tenantPredicate(TenantId) ON dbo.Appointments AFTER INSERT,
--     ADD BLOCK PREDICATE Security.fn_tenantPredicate(TenantId) ON dbo.Appointments AFTER UPDATE;
-- GO

-- ALTER SECURITY POLICY Security.TenantAccessPolicy
--     ADD FILTER PREDICATE Security.fn_tenantPredicate(TenantId) ON dbo.Visits,
--     ADD BLOCK PREDICATE Security.fn_tenantPredicate(TenantId) ON dbo.Visits AFTER INSERT,
--     ADD BLOCK PREDICATE Security.fn_tenantPredicate(TenantId) ON dbo.Visits AFTER UPDATE;
-- GO

-- ALTER SECURITY POLICY Security.TenantAccessPolicy
--     ADD FILTER PREDICATE Security.fn_tenantPredicate(TenantId) ON dbo.Payments,
--     ADD BLOCK PREDICATE Security.fn_tenantPredicate(TenantId) ON dbo.Payments AFTER INSERT,
--     ADD BLOCK PREDICATE Security.fn_tenantPredicate(TenantId) ON dbo.Payments AFTER UPDATE;
-- GO

-- ALTER SECURITY POLICY Security.TenantAccessPolicy
--     ADD FILTER PREDICATE Security.fn_tenantPredicate(TenantId) ON dbo.PatientDocuments,
--     ADD BLOCK PREDICATE Security.fn_tenantPredicate(TenantId) ON dbo.PatientDocuments AFTER INSERT,
--     ADD BLOCK PREDICATE Security.fn_tenantPredicate(TenantId) ON dbo.PatientDocuments AFTER UPDATE;
-- GO
