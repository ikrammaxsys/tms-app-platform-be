/*
  Add config_json column to agents table.
  Run against existing Toray_App_Platform databases.
*/
USE Toray_App_Platform;
GO

IF COL_LENGTH(N'dbo.agents', N'config_json') IS NULL
BEGIN
    ALTER TABLE dbo.agents ADD config_json NVARCHAR(MAX) NULL;
END
GO
