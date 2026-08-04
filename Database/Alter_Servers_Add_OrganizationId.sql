/*
  Add organization_id to Servers and link to Organizations.
  Run Alter_Organizations_Create.sql first.
*/
USE Toray_App_Platform;
GO

IF COL_LENGTH(N'dbo.Servers', N'organization_id') IS NULL
BEGIN
    ALTER TABLE dbo.Servers
        ADD organization_id INT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_Servers_Organization'
      AND parent_object_id = OBJECT_ID(N'dbo.Servers')
)
BEGIN
    ALTER TABLE dbo.Servers
        ADD CONSTRAINT FK_Servers_Organization FOREIGN KEY (organization_id)
            REFERENCES dbo.Organizations (id);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Servers_Organization'
      AND object_id = OBJECT_ID(N'dbo.Servers')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Servers_Organization
        ON dbo.Servers (organization_id);
END
GO
