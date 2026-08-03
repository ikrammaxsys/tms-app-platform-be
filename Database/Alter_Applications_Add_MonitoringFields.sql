/*
  Add healthcheck and log-scanning columns to Applications.
  Run against existing Toray_App_Platform databases.
*/
USE Toray_App_Platform;
GO

IF COL_LENGTH(N'dbo.Applications', N'healthcheck_url') IS NULL
BEGIN
    ALTER TABLE dbo.Applications ADD healthcheck_url VARCHAR(200) NULL;
END
GO

IF COL_LENGTH(N'dbo.Applications', N'is_healthcheck') IS NULL
BEGIN
    ALTER TABLE dbo.Applications ADD is_healthcheck INT NOT NULL CONSTRAINT DF_Applications_IsHealthcheck DEFAULT (0);
END
GO

IF COL_LENGTH(N'dbo.Applications', N'logs_path') IS NULL
BEGIN
    ALTER TABLE dbo.Applications ADD logs_path VARCHAR(200) NULL;
END
GO

IF COL_LENGTH(N'dbo.Applications', N'is_scaning_logs') IS NULL
BEGIN
    ALTER TABLE dbo.Applications ADD is_scaning_logs INT NOT NULL CONSTRAINT DF_Applications_IsScaningLogs DEFAULT (0);
END
GO
