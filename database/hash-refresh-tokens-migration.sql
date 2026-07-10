SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- Existing values are plaintext bearer credentials. Revoke them instead of
-- carrying them into the hashed-token scheme. All users must sign in again.
UPDATE dbo.Users
SET RefreshToken = NULL,
    RefreshTokenExpiry = NULL,
    UpdatedAt = SYSUTCDATETIME()
WHERE RefreshToken IS NOT NULL
   OR RefreshTokenExpiry IS NOT NULL;

COMMIT TRANSACTION;
