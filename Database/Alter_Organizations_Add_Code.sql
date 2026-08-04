/*
  Add code column to Organizations.
  Run against existing Toray_App_Platform databases.
*/
USE Toray_App_Platform;
GO

IF COL_LENGTH(N'dbo.Organizations', N'code') IS NULL
BEGIN
    ALTER TABLE dbo.Organizations
        ADD code NVARCHAR(50) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UQ_Organizations_Code'
      AND object_id = OBJECT_ID(N'dbo.Organizations')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_Organizations_Code
        ON dbo.Organizations (code)
        WHERE code IS NOT NULL;
END
GO
