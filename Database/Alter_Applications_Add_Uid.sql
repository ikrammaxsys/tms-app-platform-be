-- Add unique application uid for existing Toray_App_Platform databases.
IF COL_LENGTH(N'dbo.Applications', N'uid') IS NULL
BEGIN
    ALTER TABLE dbo.Applications ADD uid NVARCHAR(64) NULL;
END
GO

UPDATE dbo.Applications
SET uid = CONCAT(N'app-', id)
WHERE uid IS NULL OR LTRIM(RTRIM(uid)) = N'';
GO

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Applications')
      AND name = N'uid'
      AND is_nullable = 1
)
BEGIN
    ALTER TABLE dbo.Applications ALTER COLUMN uid NVARCHAR(64) NOT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Applications')
      AND name = N'UQ_Applications_Uid'
)
BEGIN
    ALTER TABLE dbo.Applications ADD CONSTRAINT UQ_Applications_Uid UNIQUE (uid);
END
GO
