-- Application log metadata tables for agent log ingestion.
-- Run against existing Toray_App_Platform databases.

IF OBJECT_ID(N'dbo.application_logs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.application_logs
    (
        id               INT           IDENTITY(1, 1) NOT NULL PRIMARY KEY,
        application_id   VARCHAR(100)  NOT NULL,
        [date]           VARCHAR(100)  NOT NULL,
        remote_base_path VARCHAR(100)  NOT NULL,
        application_name VARCHAR(100)  NOT NULL
    );

    CREATE UNIQUE INDEX UX_application_logs_app_date
        ON dbo.application_logs (application_id, [date]);
END
GO

IF OBJECT_ID(N'dbo.application_log_chunks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.application_log_chunks
    (
        id                 INT           IDENTITY(1, 1) NOT NULL PRIMARY KEY,
        application_log_id INT           NOT NULL,
        size               VARCHAR(100)  NOT NULL,
        name               VARCHAR(100)  NOT NULL,
        path               VARCHAR(100)  NOT NULL,
        CONSTRAINT FK_application_log_chunks_application_logs
            FOREIGN KEY (application_log_id) REFERENCES dbo.application_logs (id)
    );

    CREATE INDEX IX_application_log_chunks_application_log_id
        ON dbo.application_log_chunks (application_log_id);
END
GO
