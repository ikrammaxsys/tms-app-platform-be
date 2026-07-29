-- Align uptime log default with Malaysia time (UTC+8).
-- Run against existing Toray_App_Platform databases.
IF EXISTS (
    SELECT 1
    FROM sys.default_constraints
    WHERE name = N'DF_Uptime_TS'
      AND parent_object_id = OBJECT_ID(N'dbo.application_uptime_logs')
)
BEGIN
    ALTER TABLE dbo.application_uptime_logs DROP CONSTRAINT DF_Uptime_TS;
END
GO

ALTER TABLE dbo.application_uptime_logs
    ADD CONSTRAINT DF_Uptime_TS DEFAULT (DATEADD(HOUR, 8, SYSUTCDATETIME())) FOR [timestamp];
GO
