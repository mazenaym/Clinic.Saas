/*
Permissions foundation plan.

Apply manually after review. Current authorization is role-based; this plan
adds storage for future tenant/custom permission management without changing
the current JWT role mapping yet.
*/

IF OBJECT_ID(N'dbo.Permissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Permissions
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Permissions PRIMARY KEY,
        [Key] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(300) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Permissions_CreatedAt DEFAULT SYSUTCDATETIME()
    );

    CREATE UNIQUE INDEX UX_Permissions_Key ON dbo.Permissions([Key]);
END;

IF OBJECT_ID(N'dbo.RolePermissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RolePermissions
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RolePermissions PRIMARY KEY,
        [Role] SMALLINT NOT NULL,
        PermissionId UNIQUEIDENTIFIER NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_RolePermissions_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_RolePermissions_Permissions FOREIGN KEY (PermissionId) REFERENCES dbo.Permissions(Id)
    );

    CREATE UNIQUE INDEX UX_RolePermissions_Role_Permission ON dbo.RolePermissions([Role], PermissionId);
END;

IF OBJECT_ID(N'dbo.UserPermissions', N'U') IS NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = N'UX_Users_Tenant_Id')
        CREATE UNIQUE INDEX UX_Users_Tenant_Id ON dbo.Users(TenantId, Id);

    CREATE TABLE dbo.UserPermissions
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_UserPermissions PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        PermissionId UNIQUEIDENTIFIER NOT NULL,
        IsGranted BIT NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_UserPermissions_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_UserPermissions_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_UserPermissions_Users FOREIGN KEY (TenantId, UserId) REFERENCES dbo.Users(TenantId, Id),
        CONSTRAINT FK_UserPermissions_Permissions FOREIGN KEY (PermissionId) REFERENCES dbo.Permissions(Id)
    );

    CREATE UNIQUE INDEX UX_UserPermissions_Tenant_User_Permission ON dbo.UserPermissions(TenantId, UserId, PermissionId);
END;
