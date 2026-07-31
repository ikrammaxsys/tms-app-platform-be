/*
  Create server_metrics table for host agent telemetry.
  Run against existing Toray_App_Platform databases.
*/
USE Toray_App_Platform;
GO

IF OBJECT_ID(N'dbo.server_metrics', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.server_metrics
    (
        id                  INT             NOT NULL IDENTITY(1,1),
        server_id           INT             NOT NULL,
        cpu_cores           INT             NOT NULL,
        cpu_usage           DECIMAL(5,2)    NOT NULL,
        ram_total           BIGINT          NOT NULL,
        ram_usage           BIGINT          NOT NULL,
        ram_available       BIGINT          NOT NULL,
        disk_total          BIGINT          NOT NULL,
        disk_used           BIGINT          NOT NULL,
        disk_available      BIGINT          NOT NULL,
        [timestamp]         DATETIME2(0)    NOT NULL CONSTRAINT DF_ServerMetrics_TS DEFAULT (DATEADD(HOUR, 8, SYSUTCDATETIME())),
        CONSTRAINT PK_server_metrics PRIMARY KEY CLUSTERED (id),
        CONSTRAINT FK_server_metrics_Server FOREIGN KEY (server_id)
            REFERENCES dbo.Servers (id)
    );

    CREATE NONCLUSTERED INDEX IX_server_metrics_Server_Timestamp
        ON dbo.server_metrics (server_id, [timestamp] DESC);
END
GO
